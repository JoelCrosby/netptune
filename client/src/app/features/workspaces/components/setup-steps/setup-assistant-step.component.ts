import {
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import {
  AI_PROVIDERS,
  AiCredential,
  AiProvider,
} from '@core/models/ai-credential';
import { AiCredentialsService } from '@core/services/ai-credentials.service';
import { mutation } from '@core/util/mutation';
import { requireSuccess } from '@core/util/rxjs-operators';
import { LucideKeyRound } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { CheckLabelComponent } from '@static/components/check-label/check-label.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { FormErrorComponent } from '@static/components/form-error/form-error.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { MediaRowComponent } from '@static/components/media-row.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';

const keysStayHidden = $localize`:Explains how stored keys behave:Keys are encrypted and never shown again after saving.`;

@Component({
  selector: 'app-setup-assistant-step',
  imports: [
    CheckLabelComponent,
    ErrorStateComponent,
    FlatButtonComponent,
    FormErrorComponent,
    FormField,
    FormInputComponent,
    IconTileComponent,
    MediaRowComponent,
    SegmentedControlComponent,
  ],
  host: { class: 'flex flex-col gap-4' },
  template: `
    @if (loadFailed()) {
      <app-error-state
        compact
        i18n-title="Shown when the saved AI keys of a workspace cannot load"
        title="Saved keys could not be loaded"
        (retry)="retry.emit()" />
    } @else {
      @for (credential of credentials(); track credential.id) {
        <app-media-row [heading]="credential.label">
          <app-icon-tile
            mediaRowMedia
            size="medium"
            tone="purple"
            [icon]="keyIcon" />

          <span
            i18n="
              Says which key is stored for the workspace. HINT is its last
              characters
            ">
            Key ending
            <span class="font-mono"
              >…{{
                credential.secretHint // i18n(ph="HINT")
              }}</span
            >
            saved for this workspace.
          </span>

          <app-check-label
            mediaRowActions
            i18n="Shown when a provider key is stored"
            >Connected</app-check-label
          >
        </app-media-row>
      } @empty {
        <form
          class="flex flex-col gap-4"
          novalidate
          (submit)="$event.preventDefault(); save()">
          <app-segmented-control
            class="self-start"
            i18n-ariaLabel="Accessible label of the AI provider choice"
            ariaLabel="AI provider"
            [options]="providerOptions"
            [value]="provider()"
            (valueChange)="selectProvider($event)" />

          <app-form-input
            [noMargin]="true"
            [formField]="keyForm.secret"
            type="password"
            i18n-label="Label for the API key input"
            label="API key"
            i18n-placeholder="Placeholder of the API key input"
            placeholder="Paste your key"
            [hint]="hint()"
            (input)="request.clearError()">
            @if (request.error(); as message) {
              <app-form-error role="alert">{{ message }}</app-form-error>
            }
          </app-form-input>

          <button
            app-flat-button
            class="self-end max-md:w-full"
            type="submit"
            [disabled]="!canSave()">
            <span i18n="Button that stores an API key">Save Key</span>
          </button>
        </form>
      }
    }
  `,
})
export class SetupAssistantStepComponent {
  private readonly service = inject(AiCredentialsService);

  readonly credentials = input.required<AiCredential[]>();
  readonly loadFailed = input(false);

  readonly saved = output();
  readonly retry = output();

  protected readonly keyIcon = LucideKeyRound;

  protected readonly providerOptions: SegmentedOption[] = AI_PROVIDERS.map(
    (item) => ({ value: `${item.provider}`, label: item.label })
  );

  protected readonly provider = signal(`${AiProvider.anthropic}`);

  private readonly descriptor = computed(() => {
    const selected = Number(this.provider());

    const match = AI_PROVIDERS.find((item) => item.provider === selected);

    return match ?? AI_PROVIDERS[0];
  });

  protected readonly hint = computed(() => {
    return `${this.descriptor().hint}. ${keysStayHidden}`;
  });

  private readonly keyModel = signal({ secret: '' });

  protected readonly keyForm = form(this.keyModel);

  protected readonly canSave = computed(() => {
    return !!this.keyModel().secret.trim() && !this.request.pending();
  });

  protected readonly request = mutation({
    fallbackError: $localize`:Shown when saving an API key fails:The key could not be saved.`,
  });

  protected selectProvider(provider: string) {
    this.provider.set(provider);
    this.request.clearError();
  }

  protected save() {
    if (!this.canSave()) return;

    const { provider, label } = this.descriptor();
    const secret = this.keyModel().secret.trim();

    this.request.run(
      this.service
        .save({ provider, label, secret }, 'workspace')
        .pipe(requireSuccess()),
      {
        onSuccess: () => {
          this.keyForm().reset({ secret: '' });
          this.saved.emit();
        },
      }
    );
  }
}
