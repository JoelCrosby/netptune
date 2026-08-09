import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  SaveSearchCredentialRequest,
  WebSearchProvider,
} from '@core/models/search-credential';
import { searchCredentialResource } from '@core/resources/search-credential.resource';
import { ConfirmationService } from '@core/services/confirmation.service';
import { SearchCredentialsService } from '@core/services/search-credentials.service';
import { LucideCheck, LucideGlobe, LucideTrash } from '@lucide/angular';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { first, switchMap } from 'rxjs';

interface ProviderOption {
  provider: WebSearchProvider;
  label: string;
  hint: string;
  needsKey: boolean;
  needsEngineId: boolean;
  needsEndpoint: boolean;
}

@Component({
  selector: 'app-search-credential',
  imports: [
    FormsModule,
    LucideCheck,
    LucideTrash,
    IconTileComponent,
    DropdownButtonComponent,
    FlatButtonComponent,
    IconButtonComponent,
    MenuItemComponent,
    TooltipDirective,
  ],
  template: `
    <p
      class="text-muted max-w-3xl text-sm"
      i18n="Explains what the workspace search provider is for">
      The assistant searches the web through this provider. Without one it can
      still read pages it is given a link to, but it cannot find them itself.
    </p>

    <article
      class="border-border bg-card mt-6 overflow-hidden rounded-lg border shadow-sm">
      <header
        class="border-border flex flex-wrap items-start justify-between gap-4 border-b px-6 py-5">
        <div class="flex min-w-0 items-start gap-3">
          <app-icon-tile [icon]="providerIcon" />
          <div class="min-w-0">
            <h4 class="font-overpass text-base font-semibold">
              {{ selectedOption().label }}
            </h4>
            @if (stored(); as credential) {
              @if (credential.secretHint) {
                <p class="text-muted text-sm">
                  <span i18n="Label preceding the masked API key"
                    >Key ending</span
                  >
                  <span class="font-mono">…{{ credential.secretHint }}</span>
                </p>
              } @else {
                <p class="text-muted text-sm">{{ credential.endpoint }}</p>
              }
            } @else {
              <p class="text-muted text-sm">{{ selectedOption().hint }}</p>
            }
          </div>
        </div>

        @if (stored()) {
          <button
            app-icon-button
            type="button"
            appTooltip
            i18n-appTooltip="
              Tooltip on the button that removes the search provider
            "
            appTooltip="Remove provider"
            (click)="remove()">
            <svg lucideTrash class="h-4 w-4"></svg>
          </button>
        }
      </header>

      <div class="flex flex-wrap items-end gap-3 px-6 py-5">
        <div class="flex min-w-56 flex-col gap-1">
          <span class="text-muted text-xs" i18n="Label for the search provider">
            Provider
          </span>
          <app-dropdown-button
            #providerMenu
            [label]="selectedOption().label"
            i18n-ariaLabel="Accessible label for the search provider selector"
            ariaLabel="Search provider"
            buttonClass="h-9 min-w-56 justify-between">
            @for (option of providerOptions; track option.provider) {
              <button
                app-menu-item
                type="button"
                role="menuitemradio"
                [attr.aria-checked]="provider() === option.provider"
                (click)="setProvider(option.provider); providerMenu.close()">
                <span class="flex h-4 w-4 items-center justify-center">
                  @if (provider() === option.provider) {
                    <svg lucideCheck class="h-4 w-4"></svg>
                  }
                </span>
                <span>{{ option.label }}</span>
              </button>
            }
          </app-dropdown-button>
        </div>

        @if (selectedOption().needsKey) {
          <label class="flex min-w-56 flex-1 flex-col gap-1">
            <span class="text-muted text-xs" i18n="Label for the API key input">
              API key
            </span>
            <input
              type="password"
              name="search-secret"
              class="border-border bg-background placeholder:text-muted h-9 w-full rounded border px-3 outline-none"
              autocomplete="off"
              spellcheck="false"
              [placeholder]="secretPlaceholder()"
              [ngModel]="secret()"
              (ngModelChange)="secret.set($event)" />
          </label>
        }

        @if (selectedOption().needsEngineId) {
          <label class="flex min-w-56 flex-1 flex-col gap-1">
            <span
              class="text-muted text-xs"
              i18n="Label for the Google search engine id input">
              Search engine id
            </span>
            <input
              type="text"
              name="search-engine-id"
              class="border-border bg-background placeholder:text-muted h-9 w-full rounded border px-3 outline-none"
              autocomplete="off"
              spellcheck="false"
              i18n-placeholder="
                Placeholder for the Google search engine id input
              "
              placeholder="a1b2c3d4e5f6g7h8i"
              [ngModel]="engineId()"
              (ngModelChange)="setEngineId($event)" />
          </label>
        }

        @if (selectedOption().needsEndpoint) {
          <label class="flex min-w-56 flex-1 flex-col gap-1">
            <span
              class="text-muted text-xs"
              i18n="Label for the SearXNG base URL input">
              Base URL
            </span>
            <input
              type="url"
              name="search-endpoint"
              class="border-border bg-background placeholder:text-muted h-9 w-full rounded border px-3 outline-none"
              autocomplete="off"
              spellcheck="false"
              i18n-placeholder="Placeholder for the SearXNG base URL input"
              placeholder="https://searxng.example.com"
              [ngModel]="endpoint()"
              (ngModelChange)="setEndpoint($event)" />
          </label>
        }

        <button
          app-flat-button
          type="button"
          [disabled]="!canSave()"
          (click)="save()">
          @if (stored()) {
            <span i18n="Button that updates the stored search provider">
              Update provider
            </span>
          } @else {
            <span i18n="Button that stores the search provider">
              Save provider
            </span>
          }
        </button>
      </div>
    </article>
  `,
})
export class SearchCredentialComponent {
  protected readonly providerIcon = LucideGlobe;

  protected readonly providerOptions: ProviderOption[] = [
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

  private readonly credential = searchCredentialResource();
  private readonly service = inject(SearchCredentialsService);
  private readonly snackbar = inject(SnackbarService);
  private readonly confirmation = inject(ConfirmationService);

  private readonly pendingProvider = signal<WebSearchProvider | null>(null);
  private readonly pendingEngineId = signal<string | null>(null);
  private readonly pendingEndpoint = signal<string | null>(null);

  protected readonly secret = signal('');
  protected readonly saving = signal(false);

  protected readonly stored = computed(() => this.credential.value());

  protected readonly provider = computed(() => {
    return (
      this.pendingProvider() ??
      this.stored()?.provider ??
      WebSearchProvider.brave
    );
  });

  protected readonly selectedOption = computed(() => {
    const provider = this.provider();
    const option = this.providerOptions.find(
      (item) => item.provider === provider
    );

    return option ?? this.providerOptions[0];
  });

  protected readonly engineId = computed(() => {
    return this.pendingEngineId() ?? this.stored()?.engineId ?? '';
  });

  protected readonly endpoint = computed(() => {
    return this.pendingEndpoint() ?? this.stored()?.endpoint ?? '';
  });

  protected setEngineId(value: string) {
    this.pendingEngineId.set(value);
  }

  protected setEndpoint(value: string) {
    this.pendingEndpoint.set(value);
  }

  protected setProvider(provider: WebSearchProvider) {
    this.pendingProvider.set(provider);
  }

  protected secretPlaceholder(): string {
    const stored = this.stored();

    if (stored?.secretHint && stored.provider === this.provider()) {
      return $localize`:Placeholder shown when a key is already stored:Enter a new key to replace the stored one`;
    }

    return $localize`:Placeholder shown when no key is stored:Paste your API key`;
  }

  protected canSave(): boolean {
    if (this.saving()) {
      return false;
    }

    const option = this.selectedOption();
    const stored = this.stored();
    const keepsStoredKey =
      stored?.provider === option.provider && stored.secretHint.length > 0;
    const hasKey = this.secret().trim().length > 0 || keepsStoredKey;

    if (option.needsKey && !hasKey) {
      return false;
    }

    if (option.needsEngineId && this.engineId().trim().length === 0) {
      return false;
    }

    return !option.needsEndpoint || this.endpoint().trim().length > 0;
  }

  protected save() {
    const option = this.selectedOption();
    const secret = this.secret().trim();
    const request: SaveSearchCredentialRequest = {
      provider: option.provider,
      secret: secret.length > 0 ? secret : null,
      engineId: option.needsEngineId ? this.engineId().trim() : null,
      endpoint: option.needsEndpoint ? this.endpoint().trim() : null,
    };

    this.saving.set(true);

    this.service
      .save(request)
      .pipe(first())
      .subscribe({
        next: (response) => {
          this.saving.set(false);

          if (!response.isSuccess) {
            this.snackbar.open(
              response.message ??
                $localize`:Shown when saving the search provider fails:The provider could not be saved.`
            );

            return;
          }

          this.reset();
          this.credential.reload();
          this.snackbar.success(
            $localize`:Shown after the search provider is stored:Search provider saved.`
          );
        },
        error: () => {
          this.saving.set(false);
          this.snackbar.open(
            $localize`:Shown when saving the search provider fails:The provider could not be saved.`
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
          this.reset();
          this.credential.reload();
          this.snackbar.success(
            $localize`:Shown after the search provider is removed:Search provider removed.`
          );
        },
        error: () => {
          this.snackbar.open(
            $localize`:Shown when removing the search provider fails:The provider could not be removed.`
          );
        },
      });
  }

  private reset() {
    this.secret.set('');
    this.pendingProvider.set(null);
    this.pendingEngineId.set(null);
    this.pendingEndpoint.set(null);
  }
}
