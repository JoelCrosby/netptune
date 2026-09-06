import { Component, computed, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { AiCredential, AiProvider } from '@core/models/ai-credential';
import { SearchCredential } from '@core/models/search-credential';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { LucidePlus, LucideShield } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-assistant-header-actions',
  imports: [
    DropdownMenuComponent,
    FlatButtonComponent,
    LucidePlus,
    LucideShield,
    MenuItemComponent,
    RouterLink,
    StrokedButtonComponent,
  ],
  host: { class: 'flex items-center gap-2' },
  template: `
    @if (canReadAudit()) {
      <a
        app-stroked-button
        color="neutral"
        class="h-[34px] gap-2 px-3 text-[13px]"
        [routerLink]="auditLink()">
        <svg lucideShield class="h-3.5 w-3.5"></svg>
        <span i18n="Link to the workspace audit log">Audit log</span>
      </a>
    }

    @if (canAddConnection()) {
      <button
        app-flat-button
        type="button"
        class="h-[34px] gap-2 px-3 text-[13px]"
        (click)="connectionMenu.toggle($any($event.currentTarget))">
        <svg lucidePlus class="h-3.5 w-3.5"></svg>
        <span i18n="Button that adds an assistant connection">
          Add connection
        </span>
      </button>

      <app-dropdown-menu #connectionMenu xPosition="before">
        @if (!anthropicCredential()) {
          <button
            app-menu-item
            type="button"
            (click)="addProvider.emit(anthropic); connectionMenu.close()">
            <span i18n="Name of the Anthropic AI provider">Anthropic</span>
          </button>
        }
        @if (!openAiCredential()) {
          <button
            app-menu-item
            type="button"
            (click)="addProvider.emit(openAi); connectionMenu.close()">
            <span i18n="Name of the OpenAI provider">OpenAI</span>
          </button>
        }
        @if (!searchCredential()) {
          <button
            app-menu-item
            type="button"
            (click)="addSearch.emit(); connectionMenu.close()">
            <span i18n="Name of the assistant web search connection">
              Web search
            </span>
          </button>
        }
      </app-dropdown-menu>
    }
  `,
})
export class AssistantHeaderActionsComponent {
  readonly credentials = input.required<readonly AiCredential[]>();
  readonly searchCredential = input.required<SearchCredential | null>();
  readonly canAddConnection = input.required<boolean>();

  readonly addProvider = output<AiProvider>();
  readonly addSearch = output();

  protected readonly anthropic = AiProvider.anthropic;
  protected readonly openAi = AiProvider.openAi;

  private readonly workspaceIdentifier = inject(CurrentWorkspaceService).slug;

  protected readonly canReadAudit = hasPermission(PERMISSIONS.audit.read);

  protected readonly auditLink = computed(() => {
    return ['/', this.workspaceIdentifier() ?? '', 'audit'];
  });

  protected readonly anthropicCredential = computed(() => {
    return this.credentialFor(AiProvider.anthropic);
  });

  protected readonly openAiCredential = computed(() => {
    return this.credentialFor(AiProvider.openAi);
  });

  private credentialFor(provider: AiProvider) {
    return this.credentials().find(
      (credential) => credential.provider === provider
    );
  }
}
