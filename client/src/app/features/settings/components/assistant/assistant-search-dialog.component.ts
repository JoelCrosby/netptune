import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  SaveSearchCredentialRequest,
  SearchCredential,
  WebSearchProvider,
} from '@core/models/search-credential';
import { ConfirmationService } from '@core/services/confirmation.service';
import { SearchCredentialsService } from '@core/services/search-credentials.service';
import { getErrorMessage } from '@core/util/error-message';
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
import { first, switchMap } from 'rxjs';

export interface AssistantSearchDialogData {
  credential: SearchCredential | null;
}

interface SearchProviderOption {
  provider: WebSearchProvider;
  label: string;
  hint: string;
  needsKey: boolean;
  needsEngineId: boolean;
  needsEndpoint: boolean;
}

@Component({
  selector: 'app-assistant-search-dialog',
  imports: [
    CalloutComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    DialogTitleComponent,
    DropdownButtonComponent,
    FlatButtonComponent,
    FormsModule,
    LucideCheck,
    MenuItemComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title showCloseButton>{{ heading() }}</app-dialog-title>

    <div class="flex flex-col gap-5">
      <div class="flex flex-col gap-1.5">
        <span class="text-muted text-xs" i18n="Label for the search provider">
          Provider
        </span>
        <app-dropdown-button
          #providerMenu
          [label]="selectedOption().label"
          i18n-ariaLabel="Accessible label for the search provider selector"
          ariaLabel="Search provider"
          buttonClass="h-9 w-full justify-between">
          @for (option of providerOptions; track option.provider) {
            <button
              app-menu-item
              type="button"
              role="menuitemradio"
              [attr.aria-checked]="provider() === option.provider"
              (click)="provider.set(option.provider); providerMenu.close()">
              <span class="flex h-4 w-4 items-center justify-center">
                @if (provider() === option.provider) {
                  <svg lucideCheck class="h-4 w-4"></svg>
                }
              </span>
              <span>{{ option.label }}</span>
            </button>
          }
        </app-dropdown-button>
        <span class="text-muted text-xs">{{ selectedOption().hint }}</span>
      </div>

      @if (selectedOption().needsKey) {
        <label class="flex flex-col gap-1.5">
          <span class="text-muted text-xs" i18n="Label for the API key input">
            API key
          </span>
          <input
            type="password"
            name="assistant-search-secret"
            class="border-border bg-background placeholder:text-muted h-9 w-full rounded border px-3 outline-none"
            autocomplete="off"
            spellcheck="false"
            [placeholder]="secretPlaceholder()"
            [ngModel]="secret()"
            (ngModelChange)="secret.set($event)" />
        </label>
      }

      @if (selectedOption().needsEngineId) {
        <label class="flex flex-col gap-1.5">
          <span
            class="text-muted text-xs"
            i18n="Label for the Google search engine id input">
            Search engine id
          </span>
          <input
            type="text"
            name="assistant-search-engine-id"
            class="border-border bg-background placeholder:text-muted h-9 w-full rounded border px-3 outline-none"
            autocomplete="off"
            spellcheck="false"
            i18n-placeholder="Placeholder for the Google search engine id input"
            placeholder="a1b2c3d4e5f6g7h8i"
            [ngModel]="engineId()"
            (ngModelChange)="pendingEngineId.set($event)" />
        </label>
      }

      @if (selectedOption().needsEndpoint) {
        <label class="flex flex-col gap-1.5">
          <span
            class="text-muted text-xs"
            i18n="Label for the SearXNG base URL input">
            Base URL
          </span>
          <input
            type="url"
            name="assistant-search-endpoint"
            class="border-border bg-background placeholder:text-muted h-9 w-full rounded border px-3 outline-none"
            autocomplete="off"
            spellcheck="false"
            i18n-placeholder="Placeholder for the SearXNG base URL input"
            placeholder="https://searxng.example.com"
            [ngModel]="endpoint()"
            (ngModelChange)="pendingEndpoint.set($event)" />
        </label>
      }

      <app-callout [icon]="infoIcon" color="primary">
        <p i18n="Explains what the assistant does without a search provider">
          Without a provider the assistant can still read pages it is given a
          link to, but it cannot find them itself.
        </p>
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
          <span i18n="Button that removes the search provider">
            Remove provider
          </span>
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
export class AssistantSearchDialogComponent {
  private readonly data = inject<AssistantSearchDialogData>(DIALOG_DATA);
  private readonly dialogRef =
    inject<DialogRef<boolean, AssistantSearchDialogComponent>>(DialogRef);

  private readonly service = inject(SearchCredentialsService);
  private readonly snackbar = inject(SnackbarService);
  private readonly confirmation = inject(ConfirmationService);

  protected readonly providerOptions: SearchProviderOption[] = [
    {
      provider: WebSearchProvider.brave,
      label: 'Brave Search',
      hint: $localize`:Hint describing the Brave search provider:An API key from the Brave Search API.`,
      needsKey: true,
      needsEngineId: false,
      needsEndpoint: false,
    },
    {
      provider: WebSearchProvider.google,
      label: 'Google Programmable Search',
      hint: $localize`:Hint describing the Google search provider:An API key and the id of a programmable search engine.`,
      needsKey: true,
      needsEngineId: true,
      needsEndpoint: false,
    },
    {
      provider: WebSearchProvider.searxng,
      label: 'SearXNG',
      hint: $localize`:Hint describing the SearXNG search provider:A self-hosted instance with JSON output enabled. No key needed.`,
      needsKey: false,
      needsEngineId: false,
      needsEndpoint: true,
    },
  ];

  protected readonly infoIcon = LucideInfo;
  protected readonly credential = signal(this.data.credential);
  protected readonly secret = signal('');
  protected readonly busy = signal(false);
  protected readonly pendingEngineId = signal<string | null>(null);
  protected readonly pendingEndpoint = signal<string | null>(null);

  protected readonly provider = signal(
    this.data.credential?.provider ?? WebSearchProvider.brave
  );

  protected readonly selectedOption = computed(() => {
    const provider = this.provider();
    const option = this.providerOptions.find(
      (item) => item.provider === provider
    );

    return option ?? this.providerOptions[0];
  });

  protected readonly engineId = computed(() => {
    return this.pendingEngineId() ?? this.credential()?.engineId ?? '';
  });

  protected readonly endpoint = computed(() => {
    return this.pendingEndpoint() ?? this.credential()?.endpoint ?? '';
  });

  protected readonly heading = computed(() => {
    if (this.credential()) {
      return $localize`:Title of the dialog that manages web search:Manage web search`;
    }

    return $localize`:Title of the dialog that connects web search:Connect web search`;
  });

  protected readonly saveLabel = computed(() => {
    if (this.credential()) {
      return $localize`:Button that updates the stored search provider:Update provider`;
    }

    return $localize`:Button that stores the search provider:Save provider`;
  });

  protected readonly secretPlaceholder = computed(() => {
    const stored = this.credential();
    const keepsStoredKey =
      !!stored?.secretHint && stored.provider === this.provider();

    if (keepsStoredKey) {
      return $localize`:Placeholder shown when a key is already stored:Enter a new key to replace the stored one`;
    }

    return $localize`:Placeholder shown when no key is stored:Paste your API key`;
  });

  protected readonly canSave = computed(() => {
    if (this.busy()) {
      return false;
    }

    const option = this.selectedOption();
    const stored = this.credential();
    const keepsStoredKey =
      stored?.provider === option.provider && !!stored?.secretHint;
    const hasKey = this.secret().trim().length > 0 || keepsStoredKey;

    if (option.needsKey && !hasKey) {
      return false;
    }

    if (option.needsEngineId && this.engineId().trim().length === 0) {
      return false;
    }

    return !option.needsEndpoint || this.endpoint().trim().length > 0;
  });

  protected save() {
    const option = this.selectedOption();
    const secret = this.secret().trim();
    const request: SaveSearchCredentialRequest = {
      provider: option.provider,
      secret: secret.length > 0 ? secret : null,
      engineId: option.needsEngineId ? this.engineId().trim() : null,
      endpoint: option.needsEndpoint ? this.endpoint().trim() : null,
    };

    this.busy.set(true);

    this.service
      .save(request)
      .pipe(first())
      .subscribe({
        next: (response) => {
          this.busy.set(false);

          if (!response.isSuccess) {
            this.snackbar.open(
              response.message ??
                $localize`:Shown when saving the search provider fails:The provider could not be saved.`
            );

            return;
          }

          this.snackbar.success(
            $localize`:Shown after the search provider is stored:Search provider saved.`
          );
          this.dialogRef.close(true);
        },
        error: (error) => {
          this.busy.set(false);
          this.snackbar.open(
            getErrorMessage(
              error,
              $localize`:Shown when saving the search provider fails:The provider could not be saved.`
            )
          );
        },
      });
  }

  protected remove() {
    this.confirmation
      .open({
        title: $localize`:Title of the remove search provider confirmation:Remove search provider?`,
        message: $localize`:Explains what removing the search provider does:The assistant will stop being able to search the web. It can still read pages from a link.`,
        acceptLabel: $localize`:Confirms removing the search provider:Remove`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(
        first(),
        switchMap((confirmed) => {
          if (!confirmed) {
            return [];
          }

          return this.service.delete();
        })
      )
      .subscribe({
        next: () => {
          this.snackbar.success(
            $localize`:Shown after the search provider is removed:Search provider removed.`
          );
          this.dialogRef.close(true);
        },
        error: () => {
          this.snackbar.open(
            $localize`:Shown when removing the search provider fails:The provider could not be removed.`
          );
        },
      });
  }
}
