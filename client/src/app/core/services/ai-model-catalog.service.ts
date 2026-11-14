import { Service, computed, inject, signal } from '@angular/core';
import { AiProvider } from '@core/models/ai-credential';
import { AiEffort } from '@core/models/ai-effort';
import { AiModelOption } from '@core/models/ai-model';
import { ASSISTANT_MODEL } from '@core/models/user-preferences';
import { AiApiService } from '@core/services/ai-api.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { SessionService } from '@core/services/session.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { onChange } from '@core/util/signals';

@Service()
export class AiModelCatalogService {
  private readonly api = inject(AiApiService);
  private readonly preferences = inject(UserPreferencesService);
  private readonly isAssistantAvailable =
    inject(SessionService).isAssistantAvailable;

  readonly models = signal<AiModelOption[]>([]);
  readonly hasCredentials = signal<boolean | null>(null);

  /** The model the user picked, where null means automatic — not the model a conversation resolved to. */
  readonly selectedModel = signal<string | null>(this.readPreference());

  readonly selectedModelLabel = computed(() => {
    const selected = this.selectedModel();
    const model = this.models().find((option) => option.id === selected);

    if (model) {
      return model.label;
    }

    if (selected) {
      return selected;
    }

    return $localize`:Model option that lets the server choose:Automatic`;
  });

  // Mirrors the server, which picks Anthropic for automatic whenever a key for it exists.
  private readonly automaticModel = computed(() => {
    const defaults = this.models().filter((option) => option.isDefault);
    const anthropic = defaults.find(
      (option) => option.provider === AiProvider.anthropic
    );

    return anthropic ?? defaults[0];
  });

  readonly availableEfforts = computed<AiEffort[]>(() => {
    const selected = this.selectedModel();
    const model =
      selected === null
        ? this.automaticModel()
        : this.models().find((option) => option.id === selected);

    return model?.efforts ?? [];
  });

  private generation = 0;
  private wasRequested = false;

  constructor() {
    onChange(inject(CurrentWorkspaceService).id, (id) => this.forget(id));
  }

  async load() {
    await this.preferences.loadAndWait();

    const hasModels = this.models().length > 0;

    if (hasModels) {
      return;
    }

    this.wasRequested = true;

    const generation = this.generation;
    const [catalog, availability] = await Promise.all([
      this.api.listModels(),
      this.api.readCredentialAvailability(),
    ]);

    const isFromEarlierWorkspace = generation !== this.generation;

    if (isFromEarlierWorkspace) {
      return;
    }

    const providers = new Set(
      (availability?.providers ?? []).map((item) => item.provider)
    );
    const available = catalog.filter((model) => {
      return providers.has(model.provider);
    });

    this.hasCredentials.set(providers.size > 0);
    this.models.set(available);
    this.resetToPreference();
    this.dropUnavailableModel(available);
  }

  select(modelId: string | null) {
    this.selectedModel.set(modelId);
    this.preferences
      .updateValue(ASSISTANT_MODEL, 'global', modelId)
      .subscribe({ error: () => null });
  }

  use(modelId: string | null) {
    this.selectedModel.set(modelId);
  }

  resetToPreference() {
    this.selectedModel.set(this.readPreference());
  }

  private forget(workspaceId: number | undefined) {
    this.generation += 1;
    this.models.set([]);
    this.hasCredentials.set(null);
    this.resetToPreference();

    const shouldReload =
      this.wasRequested &&
      workspaceId !== undefined &&
      this.isAssistantAvailable();

    if (shouldReload) {
      // Permissions for the new workspace land after its id, so this can be refused.
      void this.load().catch(() => null);
    }
  }

  /** A key can be removed after its model was picked, leaving a preference the server would reject. */
  private dropUnavailableModel(available: AiModelOption[]) {
    const selected = this.selectedModel();
    const isMissing =
      selected !== null && !available.some((model) => model.id === selected);

    if (!isMissing) {
      return;
    }

    this.select(null);
  }

  private readPreference(): string | null {
    const value = this.preferences.effectiveValueFor(ASSISTANT_MODEL);

    return typeof value === 'string' ? value : null;
  }
}
