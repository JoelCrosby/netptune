import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AiCredential,
  AiProvider,
  SaveAiCredentialRequest,
} from '@core/models/ai-credential';
import { AiModelOption } from '@core/models/ai-model';
import { aiCredentialResource } from '@core/resources/ai-credential.resource';
import { aiModelResource } from '@core/resources/ai-model.resource';
import { AiCredentialsService } from '@core/services/ai-credentials.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { LucideCheck, LucideKeyRound, LucideTrash } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { first, switchMap } from 'rxjs';

interface ProviderOption {
  provider: AiProvider;
  label: string;
  hint: string;
}

const PROVIDER_OPTIONS: ProviderOption[] = [
  {
    provider: AiProvider.anthropic,
    label: $localize`:Name of the Anthropic AI provider:Anthropic`,
    hint: $localize`:Where to find an Anthropic API key:Create a key at console.anthropic.com`,
  },
  {
    provider: AiProvider.openAi,
    label: $localize`:Name of the OpenAI provider:OpenAI`,
    hint: $localize`:Where to find an OpenAI API key:Create a key at platform.openai.com`,
  },
];

@Component({
  selector: 'app-ai-credentials',
  imports: [
    FormsModule,
    LucideCheck,
    LucideKeyRound,
    LucideTrash,
    DropdownButtonComponent,
    FlatButtonComponent,
    IconButtonComponent,
    MenuItemComponent,
    TooltipDirective,
  ],
  template: `
    <p class="text-muted max-w-3xl text-sm" i18n="Explains why a key is needed">
      The assistant uses your own provider API key. Keys are encrypted, are
      never shown again after saving, and are only used for requests you start.
    </p>

    <div class="mt-6 flex flex-col gap-4">
      @for (option of providerOptions(); track option.provider) {
        <article class="border-border bg-card rounded border p-5 shadow-sm">
          <div class="flex flex-wrap items-start justify-between gap-4">
            <div class="flex min-w-0 items-start gap-3">
              <div
                class="bg-primary/10 text-primary flex h-10 w-10 shrink-0 items-center justify-center rounded">
                <svg lucideKeyRound class="h-5 w-5"></svg>
              </div>
              <div class="min-w-0">
                <h4 class="font-overpass text-[1.05rem] font-normal">
                  {{ option.label }}
                </h4>
                @if (credentialFor(option.provider); as credential) {
                  <p class="text-muted text-sm">
                    <span i18n="Label preceding the masked API key"
                      >Key ending</span
                    >
                    <span class="font-mono">…{{ credential.secretHint }}</span>
                  </p>
                } @else {
                  <p class="text-muted text-sm">{{ option.hint }}</p>
                }
              </div>
            </div>

            @if (credentialFor(option.provider); as credential) {
              <button
                app-icon-button
                type="button"
                appTooltip
                i18n-appTooltip="
                  Tooltip on the button that removes a stored API key
                "
                appTooltip="Remove key"
                (click)="remove(credential)">
                <svg lucideTrash class="h-4 w-4"></svg>
              </button>
            }
          </div>

          <div class="mt-4 flex flex-wrap items-end gap-3">
            <label class="flex min-w-56 flex-1 flex-col gap-1">
              <span
                class="text-muted text-xs"
                i18n="Label for the API key input">
                API key
              </span>
              <input
                type="password"
                class="border-border bg-background placeholder:text-muted h-9 w-full rounded border px-3 outline-none"
                autocomplete="off"
                spellcheck="false"
                [placeholder]="secretPlaceholder(option.provider)"
                [ngModel]="secretFor(option.provider)"
                (ngModelChange)="setSecret(option.provider, $event)"
                [name]="'secret-' + option.provider" />
            </label>

            <div class="flex min-w-56 flex-col gap-1">
              <span class="text-muted text-xs" i18n="Label for the model input">
                Model
              </span>
              <app-dropdown-button
                #modelMenu
                [label]="modelLabel(option.provider)"
                i18n-ariaLabel="
                  Accessible label for the assistant model selector
                "
                ariaLabel="Assistant model"
                buttonClass="h-9 min-w-56 justify-between">
                <button
                  app-menu-item
                  type="button"
                  role="menuitemradio"
                  [attr.aria-checked]="modelFor(option.provider) === ''"
                  (click)="setModel(option.provider, ''); modelMenu.close()">
                  <span class="flex h-4 w-4 items-center justify-center">
                    @if (modelFor(option.provider) === '') {
                      <svg lucideCheck class="h-4 w-4"></svg>
                    }
                  </span>
                  <span i18n="Model option that defers to the server default"
                    >Default</span
                  >
                </button>
                @for (model of modelsFor(option.provider); track model.id) {
                  <button
                    app-menu-item
                    type="button"
                    role="menuitemradio"
                    [attr.aria-checked]="modelFor(option.provider) === model.id"
                    (click)="
                      setModel(option.provider, model.id); modelMenu.close()
                    ">
                    <span class="flex h-4 w-4 items-center justify-center">
                      @if (modelFor(option.provider) === model.id) {
                        <svg lucideCheck class="h-4 w-4"></svg>
                      }
                    </span>
                    <span>{{ model.label }}</span>
                  </button>
                }
              </app-dropdown-button>
            </div>

            <button
              app-flat-button
              type="button"
              [disabled]="!canSave(option.provider)"
              (click)="save(option)">
              @if (credentialFor(option.provider)) {
                <span i18n="Button that replaces a stored API key"
                  >Replace key</span
                >
              } @else {
                <span i18n="Button that stores an API key">Save key</span>
              }
            </button>
          </div>
        </article>
      }
    </div>
  `,
})
export class AiCredentialsComponent {
  private readonly credentials = aiCredentialResource();
  private readonly catalog = aiModelResource();
  private readonly service = inject(AiCredentialsService);
  private readonly snackbar = inject(SnackbarService);
  private readonly confirmation = inject(ConfirmationService);

  private readonly secrets = signal<Record<number, string>>({});
  private readonly models = signal<Record<number, string>>({});
  private readonly saving = signal<AiProvider | null>(null);

  protected readonly providerOptions = computed(() => PROVIDER_OPTIONS);

  protected credentialFor(provider: AiProvider): AiCredential | undefined {
    return this.credentials.value().find((item) => item.provider === provider);
  }

  protected secretFor(provider: AiProvider): string {
    return this.secrets()[provider] ?? '';
  }

  protected setSecret(provider: AiProvider, secret: string) {
    this.secrets.update((current) => ({ ...current, [provider]: secret }));
  }

  protected modelFor(provider: AiProvider): string {
    const pending = this.models()[provider];

    if (pending !== undefined) {
      return pending;
    }

    return this.credentialFor(provider)?.model ?? '';
  }

  protected setModel(provider: AiProvider, model: string) {
    this.models.update((current) => ({ ...current, [provider]: model }));
  }

  protected modelsFor(provider: AiProvider): AiModelOption[] {
    return this.catalog.value().filter((model) => model.provider === provider);
  }

  protected modelLabel(provider: AiProvider): string {
    const selected = this.modelFor(provider);
    const model = this.modelsFor(provider).find(
      (option) => option.id === selected
    );

    if (model) {
      return model.label;
    }

    return $localize`:Model option that defers to the server default:Default`;
  }

  private clearPendingModel(provider: AiProvider) {
    this.models.update((current) => {
      const entries = Object.entries(current).filter(([key]) => {
        return Number(key) !== provider;
      });

      return Object.fromEntries(entries);
    });
  }

  protected secretPlaceholder(provider: AiProvider): string {
    const credential = this.credentialFor(provider);

    if (credential) {
      return $localize`:Placeholder shown when a key is already stored:Enter a new key to replace the stored one`;
    }

    return $localize`:Placeholder shown when no key is stored:Paste your API key`;
  }

  protected canSave(provider: AiProvider): boolean {
    const hasSecret = this.secretFor(provider).trim().length > 0;

    return hasSecret && this.saving() !== provider;
  }

  protected save(option: ProviderOption) {
    const secret = this.secretFor(option.provider).trim();

    if (!secret) {
      return;
    }

    const model = this.modelFor(option.provider).trim();
    const request: SaveAiCredentialRequest = {
      provider: option.provider,
      label: option.label,
      secret,
      model: model.length > 0 ? model : null,
    };

    this.saving.set(option.provider);

    this.service
      .save(request)
      .pipe(first())
      .subscribe({
        next: (response) => {
          this.saving.set(null);

          if (!response.isSuccess) {
            this.snackbar.open(
              response.message ??
                $localize`:Shown when saving an API key fails:The key could not be saved.`
            );

            return;
          }

          this.setSecret(option.provider, '');
          this.clearPendingModel(option.provider);
          this.credentials.reload();
          this.snackbar.success(
            $localize`:Shown after an API key is stored:Key saved.`
          );
        },
        error: () => {
          this.saving.set(null);
          this.snackbar.open(
            $localize`:Shown when saving an API key fails:The key could not be saved.`
          );
        },
      });
  }

  protected remove(credential: AiCredential) {
    this.confirmation
      .open({
        title: $localize`:Title of the remove API key confirmation:Remove key?`,
        message: $localize`:Explains what removing an API key does:The assistant will stop working for this provider until you add a new key.`,
        acceptLabel: $localize`:Confirms removing an API key:Remove`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(
        first(),
        switchMap((confirmed) => {
          if (!confirmed) {
            return [];
          }

          return this.service.delete(credential.id);
        })
      )
      .subscribe({
        next: () => {
          this.credentials.reload();
          this.snackbar.success(
            $localize`:Shown after an API key is removed:Key removed.`
          );
        },
        error: () => {
          this.snackbar.open(
            $localize`:Shown when removing an API key fails:The key could not be removed.`
          );
        },
      });
  }
}
