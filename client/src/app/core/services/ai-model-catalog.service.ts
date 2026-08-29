import { Service, computed, inject, signal } from '@angular/core';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { AiModelOption } from '@core/models/ai-model';
import { AiApiService } from '@core/services/ai-api.service';

const MODEL_STORAGE_KEY = 'ai-assistant.model';

@Service()
export class AiModelCatalogService {
  private readonly api = inject(AiApiService);
  private readonly storage = inject(LocalStorageService);

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

  /** Automatic resolves to a default model that supports effort, so it keeps the control visible. */
  readonly supportsEffort = computed(() => {
    const selected = this.selectedModel();

    if (selected === null) {
      return true;
    }

    const model = this.models().find((option) => option.id === selected);

    return model?.supportsEffort ?? false;
  });

  async load() {
    const hasModels = this.models().length > 0;

    if (hasModels) {
      return;
    }

    const [catalog, availability] = await Promise.all([
      this.api.listModels(),
      this.api.readCredentialAvailability(),
    ]);

    const providers = new Set(
      (availability?.providers ?? []).map((item) => item.provider)
    );
    const available = catalog.filter((model) => {
      return providers.has(model.provider);
    });

    this.hasCredentials.set(providers.size > 0);
    this.models.set(available);
    this.dropUnavailableModel(available);
  }

  select(modelId: string | null) {
    this.selectedModel.set(modelId);
    this.storage.setItem(MODEL_STORAGE_KEY, modelId);
  }

  use(modelId: string | null) {
    this.selectedModel.set(modelId);
  }

  resetToPreference() {
    this.selectedModel.set(this.readPreference());
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
    return this.storage.getItem<string | null>(MODEL_STORAGE_KEY) ?? null;
  }
}
