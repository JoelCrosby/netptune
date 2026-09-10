import { Component, computed, inject, input, output } from '@angular/core';
import { AiCredential, AiProvider } from '@core/models/ai-credential';
import { SearchCredential } from '@core/models/search-credential';
import { DialogService } from '@core/services/dialog.service';
import {
  LucideGlobe,
  LucideKeyRound,
  type LucideIconInput,
} from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { first } from 'rxjs';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import {
  AssistantConnectionDialogComponent,
  AssistantConnectionDialogData,
} from './assistant-connection-dialog.component';
import {
  AssistantSearchDialogComponent,
  AssistantSearchDialogData,
} from './assistant-search-dialog.component';

interface ProviderDescriptor {
  provider: AiProvider;
  label: string;
  hint: string;
}

export interface AssistantConnection {
  id: string;
  icon: LucideIconInput;
  name: string;
  detail: string;
  connected: boolean;
  lastUsedAt: string | null;
  actionLabel: string;
  provider: AiProvider | null;
}

const PROVIDERS: ProviderDescriptor[] = [
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

const SEARCH_PROVIDER_LABELS: Record<number, string> = {
  0: 'Brave Search',
  1: 'Google Programmable Search',
  2: 'SearXNG',
};

@Component({
  selector: 'app-assistant-connections',
  imports: [
    IconTileComponent,
    PanelComponent,
    PanelHeaderComponent,
    PrettyDatePipe,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    <section app-panel surface="card">
      <app-panel-header
        density="comfortable"
        i18n-heading="Heading of the assistant connections card"
        heading="Connections"
        i18n-description="Explains what the assistant connections are"
        description="Model providers and web search, shared by every member without a personal key." />

      <ul class="divide-border/50 flex flex-col divide-y">
        @for (connection of connections(); track connection.id) {
          <li
            class="grid grid-cols-1 items-center gap-x-4 gap-y-3 px-6 py-4 sm:grid-cols-[minmax(0,1fr)_9rem_7rem_auto]">
            <div class="flex min-w-0 items-center gap-3">
              <app-icon-tile [icon]="connection.icon" />
              <div class="min-w-0">
                <p class="truncate text-sm font-medium">
                  {{ connection.name }}
                </p>
                <p class="text-muted truncate text-xs">
                  {{ connection.detail }}
                </p>
              </div>
            </div>

            <div class="flex items-center gap-2">
              <span
                class="h-1.5 w-1.5 shrink-0 rounded-full"
                [class]="
                  connection.connected ? 'bg-primary' : 'bg-foreground/30'
                "
                aria-hidden="true"></span>
              <span class="text-sm">
                @if (connection.connected) {
                  <span i18n="Shown when a provider key is stored"
                    >Connected</span
                  >
                } @else {
                  <span
                    class="text-muted"
                    i18n="Shown when no provider key is stored"
                    >Not connected</span
                  >
                }
              </span>
            </div>

            <span class="text-muted text-xs tabular-nums">
              @if (connection.lastUsedAt; as lastUsedAt) {
                {{ toDate(lastUsedAt) | prettyDate }}
              } @else {
                —
              }
            </span>

            <div class="sm:justify-self-end">
              <button
                app-stroked-button
                color="neutral"
                type="button"
                class="h-8 px-3 text-xs"
                (click)="open(connection)">
                {{ connection.actionLabel }}
              </button>
            </div>
          </li>
        }
      </ul>
    </section>
  `,
})
export class AssistantConnectionsComponent {
  readonly credentials = input.required<AiCredential[]>();
  readonly searchCredential = input.required<SearchCredential | null>();

  readonly changed = output();

  private readonly dialog = inject(DialogService);

  private readonly manageLabel = $localize`:Button that opens a stored connection:Manage`;
  private readonly addKeyLabel = $localize`:Button that adds a provider key:Add key`;
  private readonly connectLabel = $localize`:Button that connects web search:Connect`;

  protected readonly connections = computed<AssistantConnection[]>(() => {
    const providerRows = PROVIDERS.map((descriptor) => {
      return this.toProviderRow(descriptor);
    });

    return [...providerRows, this.toSearchRow()];
  });

  protected toDate(value: string): Date {
    return new Date(value);
  }

  protected open(connection: AssistantConnection) {
    if (connection.provider === null) {
      this.openSearch();

      return;
    }

    this.openProvider(connection.provider);
  }

  private toProviderRow(descriptor: ProviderDescriptor): AssistantConnection {
    const credential = this.credentialFor(descriptor.provider);
    const keyDetail = $localize`:Describes a stored key by its last characters:Key ending …${credential?.secretHint}:hint:`;

    return {
      id: `provider-${descriptor.provider}`,
      icon: LucideKeyRound,
      name: descriptor.label,
      detail: credential ? keyDetail : descriptor.hint,
      connected: !!credential,
      lastUsedAt: credential?.lastUsedAt ?? null,
      actionLabel: credential ? this.manageLabel : this.addKeyLabel,
      provider: descriptor.provider,
    };
  }

  private toSearchRow(): AssistantConnection {
    const credential = this.searchCredential();
    const label = credential
      ? SEARCH_PROVIDER_LABELS[credential.provider]
      : $localize`:Name of the assistant web search connection:Web search`;

    return {
      id: 'search',
      icon: LucideGlobe,
      name: label,
      detail: this.searchDetail(credential),
      connected: !!credential,
      lastUsedAt: credential?.lastUsedAt ?? null,
      actionLabel: credential ? this.manageLabel : this.connectLabel,
      provider: null,
    };
  }

  private searchDetail(credential: SearchCredential | null): string {
    if (!credential) {
      return $localize`:Hint listing the supported web search providers:Brave, Google or SearXNG`;
    }

    if (credential.secretHint) {
      return $localize`:Describes a stored key by its last characters:Key ending …${credential.secretHint}:hint:`;
    }

    return credential.endpoint ?? '';
  }

  private credentialFor(provider: AiProvider): AiCredential | undefined {
    return this.credentials().find((item) => item.provider === provider);
  }

  openProvider(provider: AiProvider) {
    const descriptor = PROVIDERS.find((item) => item.provider === provider);

    if (!descriptor) {
      return;
    }

    const data: AssistantConnectionDialogData = {
      provider,
      label: descriptor.label,
      hint: descriptor.hint,
      credential: this.credentialFor(provider) ?? null,
    };

    this.dialog
      .open<boolean, AssistantConnectionDialogData>(
        AssistantConnectionDialogComponent,
        { width: '560px', data }
      )
      .closed.pipe(first())
      .subscribe((changed) => {
        if (changed) this.changed.emit();
      });
  }

  openSearch() {
    const data: AssistantSearchDialogData = {
      credential: this.searchCredential(),
    };

    this.dialog
      .open<boolean, AssistantSearchDialogData>(
        AssistantSearchDialogComponent,
        {
          width: '560px',
          data,
        }
      )
      .closed.pipe(first())
      .subscribe((changed) => {
        if (changed) this.changed.emit();
      });
  }
}
