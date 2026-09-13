import { Component, computed, inject, input } from '@angular/core';
import { ServiceAccount } from '@core/models/service-account';
import {
  LucideBot,
  LucideKeyRound,
  LucideSettings2,
  LucideX,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ListRowComponent } from '@static/components/list-row.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelComponent } from '@static/components/panel.component';
import { SectionLabelDirective } from '@static/directives/section-label.directive';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { ServiceAccountsPageService } from '../../services/service-accounts-page.service';
import { ApiCredentialRowComponent } from './api-credential-row.component';
import { ServiceAccountPermissionSummaryComponent } from './service-account-permission-summary.component';

@Component({
  selector: 'app-service-account-card',
  imports: [
    ApiCredentialRowComponent,
    BadgeComponent,
    IconButtonComponent,
    ListRowComponent,
    LucideKeyRound,
    LucideSettings2,
    LucideX,
    PanelBodyComponent,
    PanelComponent,
    PanelHeaderComponent,
    SectionLabelDirective,
    ServiceAccountPermissionSummaryComponent,
    StrokedButtonComponent,
    TooltipDirective,
  ],
  template: `
    <article app-panel surface="card">
      <app-panel-header density="comfortable" [icon]="accountIcon">
        <div panelHeading>
          <div class="flex flex-wrap items-center gap-2">
            <h3 class="font-overpass text-lg font-medium">
              {{ account().name }}
            </h3>
            @if (account().disabledAt) {
              <app-badge
                color="warn"
                i18n="Badge marking a disabled service account">
                Disabled
              </app-badge>
            } @else {
              <app-badge
                color="success"
                i18n="Badge marking an enabled service account">
                Active
              </app-badge>
            }
          </div>
          @if (account().description; as description) {
            <p class="text-muted mt-1 text-sm">{{ description }}</p>
          }
        </div>

        @if (!account().disabledAt) {
          <div panelHeaderActions class="flex items-center gap-2">
            @if (page.canManageCredentials()) {
              <button
                app-stroked-button
                type="button"
                [disabled]="page.busy()"
                (click)="page.createCredential(account())">
                <svg lucideKeyRound class="h-4 w-4"></svg>
                <span
                  i18n="
                    Button that issues a new credential for a service account
                  ">
                  Create credential
                </span>
              </button>
            }

            @if (page.canUpdate()) {
              <button
                app-icon-button
                type="button"
                i18n-appTooltip="
                  Tooltip on the button that edits a service account
                "
                appTooltip="Edit service account"
                [attr.aria-label]="editLabel()"
                [disabled]="page.busy()"
                (click)="page.editAccount(account())">
                <svg lucideSettings2 class="h-4 w-4"></svg>
              </button>
            }

            @if (page.canDelete()) {
              <button
                app-icon-button
                color="warn"
                type="button"
                i18n-appTooltip="
                  Tooltip on the button that deletes a service account
                "
                appTooltip="Delete service account"
                [attr.aria-label]="deleteLabel()"
                [disabled]="page.busy()"
                (click)="page.deleteAccount(account())">
                <svg lucideX class="h-4 w-4"></svg>
              </button>
            }
          </div>
        }
      </app-panel-header>

      <app-panel-body
        padding="snug"
        class="grid gap-6 lg:grid-cols-[minmax(0,1.35fr)_minmax(0,1fr)]">
        <app-service-account-permission-summary
          [permissions]="account().permissions" />

        <section>
          <h4 appSectionLabel class="mb-3 flex items-center gap-2">
            <svg lucideKeyRound class="h-3.5 w-3.5"></svg>
            <span i18n="Heading above a service account's credentials">
              Credentials
            </span>
          </h4>

          <ul class="flex flex-col gap-2">
            @for (credential of account().credentials; track credential.id) {
              <li app-list-row>
                <app-api-credential-row
                  [account]="account()"
                  [credential]="credential" />
              </li>
            } @empty {
              <li
                class="text-muted text-sm"
                i18n="Shown when a service account has no credentials">
                No credentials have been created.
              </li>
            }
          </ul>
        </section>
      </app-panel-body>
    </article>
  `,
})
export class ServiceAccountCardComponent {
  protected readonly page = inject(ServiceAccountsPageService);
  protected readonly accountIcon = LucideBot;

  readonly account = input.required<ServiceAccount>();

  protected readonly editLabel = computed(() => {
    const name = this.account().name;

    return $localize`:Accessible label for the button that edits a service account. NAME is the account name:Edit ${name}:NAME:`;
  });

  protected readonly deleteLabel = computed(() => {
    const name = this.account().name;

    return $localize`:Accessible label for the button that deletes a service account. NAME is the account name:Delete ${name}:NAME:`;
  });
}
