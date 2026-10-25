import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  Component,
  computed,
  inject,
  signal,
  WritableSignal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AiCredential,
  AiCredentialScope,
  AiProvider,
  SaveAiCredentialRequest,
  UpdateAiCredentialModelsRequest,
} from '@core/models/ai-credential';
import { AiModelOption } from '@core/models/ai-model';
import { aiModelResource } from '@core/resources/ai-model.resource';
import { AiCredentialsService } from '@core/services/ai-credentials.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { LucideCheck, LucideInfo } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { first, switchMap } from 'rxjs';
import { mutation } from '@core/util/mutation';
import { requireSuccess } from '@core/util/rxjs-operators';
import { FormControlFieldComponent } from '@static/components/form-control/form-control-field.component';
import { FormControlInputDirective } from '@static/components/form-control/form-control.directives';

export interface AssistantConnectionDialogData {
  scope: AiCredentialScope;
  provider: AiProvider;
  label: string;
  hint: string;
  credential: AiCredential | null;
}

interface ModelPicker {
  id: 'chat' | 'review';
  heading: string;
  ariaLabel: string;
  defaultLabel: string;
  selected: WritableSignal<string>;
}

@Component({
  selector: 'app-assistant-connection-dialog',
  imports: [
    FormControlInputDirective,
    FormControlFieldComponent,
    CalloutComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    DialogTitleComponent,
    DropdownButtonComponent,
    FlatButtonComponent,
    FormsModule,
    LucideCheck,
    MenuItemComponent,
    PrettyDatePipe,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title showCloseButton>{{ heading() }}</app-dialog-title>

    <div class="flex flex-col gap-5">
      @if (credential(); as stored) {
        <dl
          class="border-border divide-border grid grid-cols-2 divide-x divide-y overflow-hidden rounded-lg border">
          <div class="px-4 py-3">
            <dt
              class="text-muted text-xs font-semibold tracking-wide uppercase"
              i18n="Label for the masked API key in the connection dialog">
              Key
            </dt>
            <dd class="mt-1 font-mono text-sm">…{{ stored.secretHint }}</dd>
          </div>

          <div class="px-4 py-3">
            <dt
              class="text-muted text-xs font-semibold tracking-wide uppercase"
              i18n="Label for the state of a stored connection">
              Status
            </dt>
            <dd class="text-primary mt-1 text-sm font-medium">
              <span i18n="Shown when a provider key is stored">Connected</span>
            </dd>
          </div>

          <div class="px-4 py-3">
            <dt
              class="text-muted text-xs font-semibold tracking-wide uppercase"
              i18n="Label for when a key was stored">
              Added
            </dt>
            <dd class="mt-1 text-sm">
              {{ toDate(stored.createdAt) | prettyDate }}
            </dd>
          </div>

          <div class="px-4 py-3">
            <dt
              class="text-muted text-xs font-semibold tracking-wide uppercase"
              i18n="Label for when a key was last used">
              Last used
            </dt>
            <dd class="mt-1 text-sm">
              @if (stored.lastUsedAt; as lastUsedAt) {
                {{ toDate(lastUsedAt) | prettyDate }}
              } @else {
                <span class="text-muted" i18n="Shown when a key is unused"
                  >Never</span
                >
              }
            </dd>
          </div>
        </dl>
      }

      <label class="flex flex-col gap-1.5">
        <span class="text-muted text-xs">{{ secretLabel() }}</span>
        <app-form-control-field density="compact">
          <input
            appFormInput
            class="px-4"
            type="password"
            name="assistant-connection-secret"
            autocomplete="off"
            spellcheck="false"
            [placeholder]="secretPlaceholder()"
            [ngModel]="secret()"
            (ngModelChange)="secret.set($event)" />
        </app-form-control-field>
        <span class="text-muted text-xs" i18n="Explains how stored keys behave">
          Keys are encrypted and never shown again after saving.
        </span>
      </label>

      @for (picker of pickers(); track picker.id) {
        <div class="flex flex-col gap-1.5">
          <span class="text-muted text-xs">{{ picker.heading }}</span>
          <app-dropdown-button
            #modelMenu
            [label]="modelLabel(picker.selected(), picker.defaultLabel)"
            [ariaLabel]="picker.ariaLabel"
            buttonClass="h-9 w-full justify-between">
            <button
              app-menu-item
              type="button"
              role="menuitemradio"
              [attr.aria-checked]="picker.selected() === ''"
              (click)="picker.selected.set(''); modelMenu.close()">
              <span class="flex h-4 w-4 items-center justify-center">
                @if (picker.selected() === '') {
                  <svg lucideCheck class="h-4 w-4"></svg>
                }
              </span>
              <span>{{ picker.defaultLabel }}</span>
            </button>
            @for (option of models(); track option.id) {
              <button
                app-menu-item
                type="button"
                role="menuitemradio"
                [attr.aria-checked]="picker.selected() === option.id"
                (click)="picker.selected.set(option.id); modelMenu.close()">
                <span class="flex h-4 w-4 items-center justify-center">
                  @if (picker.selected() === option.id) {
                    <svg lucideCheck class="h-4 w-4"></svg>
                  }
                </span>
                <span>{{ option.label }}</span>
              </button>
            }
          </app-dropdown-button>
        </div>
      }

      <app-callout [icon]="infoIcon" color="primary">
        @if (isWorkspace) {
          <p i18n="Explains who a workspace connection serves">
            Members without a personal key use this one. Removing it stops the
            assistant for them — spend history is kept.
          </p>
        } @else {
          <p i18n="Explains who a personal connection serves">
            Only requests you start use this key, instead of any workspace key.
            Removing it falls back to the workspace key when there is one.
          </p>
        }
      </app-callout>
    </div>

    <div app-dialog-actions class="justify-between">
      @if (credential()) {
        <button
          app-flat-button
          color="ghost"
          type="button"
          class="text-warn"
          [disabled]="busy()"
          (click)="remove()">
          <span i18n="Button that removes a stored API key">Remove key</span>
        </button>
      } @else {
        <span></span>
      }

      <div class="flex gap-3">
        <button app-stroked-button app-dialog-close type="button">
          <span i18n="Dismisses a dialog without saving">Cancel</span>
        </button>
        <button
          app-flat-button
          type="button"
          [disabled]="!canSave()"
          (click)="save()">
          {{ saveLabel() }}
        </button>
      </div>
    </div>
  `,
})
export class AssistantConnectionDialogComponent {
  private readonly data = inject<AssistantConnectionDialogData>(DIALOG_DATA);
  private readonly dialogRef =
    inject<DialogRef<boolean, AssistantConnectionDialogComponent>>(DialogRef);

  private readonly service = inject(AiCredentialsService);
  private readonly snackbar = inject(SnackbarService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly catalog = aiModelResource();

  protected readonly infoIcon = LucideInfo;
  protected readonly isWorkspace = this.data.scope === 'workspace';
  protected readonly credential = signal(this.data.credential);
  protected readonly secret = signal('');
  private readonly request = mutation({
    fallbackError: $localize`:Shown when saving an API key fails:The key could not be saved.`,
  });

  protected readonly busy = this.request.pending;
  protected readonly model = signal(this.data.credential?.model ?? '');
  protected readonly reviewModel = signal(
    this.data.credential?.reviewModel ?? ''
  );

  private readonly defaultLabel = $localize`:Model option that defers to the server default:Default`;

  protected readonly models = computed<AiModelOption[]>(() => {
    return this.catalog
      .value()
      .filter((option) => option.provider === this.data.provider);
  });

  protected readonly heading = computed(() => {
    if (this.credential()) {
      return $localize`:Title of the dialog that manages a stored provider key:Manage ${this.data.label}:provider: key`;
    }

    return $localize`:Title of the dialog that adds a provider key:Add ${this.data.label}:provider: key`;
  });

  protected readonly secretLabel = computed(() => {
    if (this.credential()) {
      return $localize`:Label for the input that replaces a stored key:Replace key`;
    }

    return $localize`:Label for the API key input:API key`;
  });

  protected readonly secretPlaceholder = computed(() => {
    if (this.credential()) {
      return $localize`:Placeholder shown when a key is already stored:Enter a new key to replace the stored one`;
    }

    return this.data.hint;
  });

  protected readonly saveLabel = computed(() => {
    const isModelsOnly = this.credential() && !this.secret().trim();

    if (isModelsOnly) {
      return $localize`:Button that saves a connection's model choices:Save`;
    }

    if (this.credential()) {
      return $localize`:Button that replaces a stored API key:Replace key`;
    }

    return $localize`:Button that stores an API key:Save key`;
  });

  private readonly reviewDefaultLabel = computed(() => {
    const option = this.models().find((item) => item.isReviewDefault);

    if (!option) {
      return this.defaultLabel;
    }

    return $localize`:Review model option that uses the built-in default. MODEL is its name:Default (${option.label}:MODEL:)`;
  });

  protected readonly pickers = computed<ModelPicker[]>(() => {
    return [
      {
        id: 'chat',
        heading: $localize`:Label for the model a connection uses:Model for assistant chat`,
        ariaLabel: $localize`:Accessible label for the assistant model selector:Assistant model`,
        defaultLabel: this.defaultLabel,
        selected: this.model,
      },
      {
        id: 'review',
        heading: $localize`:Label for the model a connection uses to review tasks:Model for task reviews`,
        ariaLabel: $localize`:Accessible label for the task review model selector:Task review model`,
        defaultLabel: this.reviewDefaultLabel(),
        selected: this.reviewModel,
      },
    ];
  });

  protected readonly modelsChanged = computed(() => {
    const stored = this.credential();
    const chatChanged = this.model() !== (stored?.model ?? '');
    const reviewChanged = this.reviewModel() !== (stored?.reviewModel ?? '');

    return chatChanged || reviewChanged;
  });

  protected readonly canSave = computed(() => {
    const hasSecret = this.secret().trim().length > 0;
    const canSaveModelsOnly = !!this.credential() && this.modelsChanged();

    return (hasSecret || canSaveModelsOnly) && !this.busy();
  });

  protected modelLabel(selected: string, defaultLabel: string): string {
    const option = this.models().find((item) => item.id === selected);

    return option?.label ?? defaultLabel;
  }

  protected toDate(value: string): Date {
    return new Date(value);
  }

  protected save() {
    const secret = this.secret().trim();
    const stored = this.credential();
    const models = {
      model: this.model().trim() || null,
      reviewModel: this.reviewModel().trim() || null,
    };

    if (!secret && stored) {
      this.saveModels(stored, models);

      return;
    }

    if (!secret) {
      return;
    }

    const request: SaveAiCredentialRequest = {
      provider: this.data.provider,
      label: this.data.label,
      secret,
      ...models,
    };

    this.request.run(
      this.service.save(request, this.data.scope).pipe(requireSuccess()),
      {
        onSuccess: () => {
          this.snackbar.success(
            $localize`:Shown after an API key is stored:Key saved.`
          );
          this.dialogRef.close(true);
        },
        onError: (message) => this.snackbar.open(message),
      }
    );
  }

  private saveModels(
    stored: AiCredential,
    request: UpdateAiCredentialModelsRequest
  ) {
    const update = this.service
      .updateModels(stored.id, request, this.data.scope)
      .pipe(requireSuccess());

    this.request.run(update, {
      onSuccess: () => {
        this.snackbar.success(
          $localize`:Shown after a connection's models are saved:Models saved.`
        );
        this.dialogRef.close(true);
      },
      onError: (message) => this.snackbar.open(message),
    });
  }

  protected remove() {
    const stored = this.credential();

    if (!stored) {
      return;
    }

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

          return this.service.delete(stored.id, this.data.scope);
        })
      )
      .subscribe({
        next: () => {
          this.snackbar.success(
            $localize`:Shown after an API key is removed:Key removed.`
          );
          this.dialogRef.close(true);
        },
        error: () => {
          this.snackbar.open(
            $localize`:Shown when removing an API key fails:The key could not be removed.`
          );
        },
      });
  }
}
