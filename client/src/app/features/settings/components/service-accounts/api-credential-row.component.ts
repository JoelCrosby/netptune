import { Component, LOCALE_ID, computed, inject, input } from '@angular/core';
import { ApiCredential, ServiceAccount } from '@core/models/service-account';
import {
  LucideListChecks,
  LucideTrash,
  LucideTriangleAlert,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { ServiceAccountsPageService } from '../../services/service-accounts-page.service';
import { missingCredentialScopes } from './service-account-permissions';

type CredentialStatus = 'active' | 'expired' | 'revoked';

@Component({
  selector: 'app-api-credential-row',
  imports: [
    BadgeComponent,
    IconButtonComponent,
    LucideListChecks,
    LucideTrash,
    LucideTriangleAlert,
    TooltipDirective,
  ],
  host: { class: 'contents' },
  template: `
    <div class="min-w-0 flex-1 py-1">
      <div class="flex flex-wrap items-center gap-x-4 gap-y-1">
        <span class="truncate text-sm font-medium">{{
          credential().name
        }}</span>
        <code class="text-muted text-xs">{{ credential().tokenPrefix }}…</code>
        <app-badge [color]="status() === 'active' ? 'success' : 'warn'">
          @switch (status()) {
            @case ('revoked') {
              <ng-container i18n="Badge on a credential that has been revoked">
                Revoked
              </ng-container>
            }
            @case ('expired') {
              <ng-container
                i18n="Badge on a credential that has passed its expiry">
                Expired
              </ng-container>
            }
            @default {
              <ng-container
                i18n="Badge on a credential that is currently usable">
                Active
              </ng-container>
            }
          }
        </app-badge>
      </div>

      <p class="text-muted mt-2 text-xs">
        <span i18n="Credential expiry. DATE is a formatted date and time">
          Expires
          {{
            formatDate(credential().expiresAt) // i18n(ph="DATE")
          }}
        </span>
        @if (credential().lastUsedAt; as lastUsedAt) {
          <span
            i18n="
              When a credential was last used, shown after the expiry. Keep the
              leading separator. DATE is a formatted date and time
            ">
            · Last used
            {{
              formatDate(lastUsedAt) // i18n(ph="DATE")
            }}
          </span>
        } @else {
          <span
            i18n="
              Shown after the expiry when a credential has never been used. Keep
              the leading separator
            ">
            · Never used
          </span>
        }
      </p>

      @if (!credential().revokedAt) {
        <p class="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs">
          @if (missingScopeCount(); as missing) {
            <span class="text-warn inline-flex items-center gap-1">
              <svg lucideTriangleAlert class="h-3 w-3 shrink-0"></svg>
              <ng-container
                i18n="
                  Warns that a credential cannot use some of its service
                  account's permissions. COUNT is how many
                ">
                {missing, plural,
                  =1 {Missing 1 account permission}
                  other {Missing {{ missing }} account permissions}
                }
              </ng-container>
            </span>
            <span
              class="text-muted"
              i18n="
                How many permissions a credential's scopes cover. Keep the
                leading separator. SCOPES and TOTAL are counts
              ">
              · Scoped to
              {{
                credential().scopes.length // i18n(ph="SCOPES")
              }}
              of
              {{
                account().permissions.length // i18n(ph="TOTAL")
              }}
            </span>
          } @else {
            <span
              class="text-muted"
              i18n="
                Shown on a credential whose scopes cover every permission of its
                service account
              ">
              All account permissions
            </span>
          }
        </p>
      }
    </div>

    @if (canManage()) {
      <button
        app-icon-button
        type="button"
        i18n-appTooltip="Tooltip on the button that edits a credential's scopes"
        appTooltip="Edit scopes"
        [attr.aria-label]="editScopesLabel()"
        [disabled]="page.busy()"
        (click)="page.editCredentialScopes(account(), credential())">
        <svg lucideListChecks class="h-4 w-4"></svg>
      </button>

      <button
        app-icon-button
        color="warn"
        type="button"
        i18n-appTooltip="Tooltip on the button that revokes a credential"
        appTooltip="Revoke credential"
        [attr.aria-label]="revokeLabel()"
        [disabled]="page.busy()"
        (click)="page.revokeCredential(account(), credential())">
        <svg lucideTrash class="h-4 w-4"></svg>
      </button>
    }
  `,
})
export class ApiCredentialRowComponent {
  protected readonly page = inject(ServiceAccountsPageService);
  private readonly locale = inject(LOCALE_ID);

  readonly account = input.required<ServiceAccount>();
  readonly credential = input.required<ApiCredential>();

  protected readonly status = computed<CredentialStatus>(() => {
    const credential = this.credential();

    if (credential.revokedAt) return 'revoked';

    const expired = new Date(credential.expiresAt).getTime() <= Date.now();

    return expired ? 'expired' : 'active';
  });

  protected readonly missingScopeCount = computed(() => {
    return missingCredentialScopes(this.account(), this.credential()).length;
  });

  protected readonly canManage = computed(() => {
    const isOpen = !this.credential().revokedAt && !this.account().disabledAt;

    return this.page.canManageCredentials() && isOpen;
  });

  protected readonly editScopesLabel = computed(() => {
    const name = this.credential().name;

    return $localize`:Accessible label for the button that edits a credential's scopes. NAME is the credential name:Edit scopes for ${name}:NAME:`;
  });

  protected readonly revokeLabel = computed(() => {
    const name = this.credential().name;

    return $localize`:Accessible label for the button that revokes a credential. NAME is the credential name:Revoke ${name}:NAME:`;
  });

  protected formatDate(value: string) {
    return new Intl.DateTimeFormat(this.locale, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }
}
