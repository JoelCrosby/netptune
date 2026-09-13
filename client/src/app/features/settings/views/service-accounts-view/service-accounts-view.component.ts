import { Component, inject } from '@angular/core';
import { LucideBot, LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { PanelComponent } from '@static/components/panel.component';
import { ServiceAccountCardComponent } from '@settings/components/service-accounts/service-account-card.component';
import { ServiceAccountsPageService } from '@settings/services/service-accounts-page.service';

@Component({
  selector: 'app-service-accounts-view',
  imports: [
    EmptyStateComponent,
    ErrorStateComponent,
    FlatButtonComponent,
    LucideBot,
    LucidePlus,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    PanelComponent,
    ServiceAccountCardComponent,
  ],
  providers: [ServiceAccountsPageService],
  template: `
    <app-page-container layout="list">
      @if (page.canCreate()) {
        <app-page-header
          toolbar
          i18n-title="Page title for workspace service accounts"
          title="Service accounts"
          i18n-actionTitle="Button that opens the create-service-account dialog"
          actionTitle="Create service account"
          (actionClick)="page.createAccount()" />
      } @else {
        <app-page-header
          toolbar
          i18n-title="Page title for workspace service accounts"
          title="Service accounts" />
      }

      <app-page-body scroll>
        <p
          class="text-muted mb-4 max-w-3xl text-sm"
          i18n="Explains what service accounts are for">
          Create workspace identities for agents and integrations without
          sharing a user login.
        </p>

        @if (page.loading()) {
          <app-page-loading
            class="min-h-48"
            i18n-label="Shown while service accounts are loading"
            label="Loading service accounts" />
        } @else if (page.accounts.error()) {
          <app-error-state
            compact
            i18n-title="Shown when the service account list fails to load"
            title="Service accounts could not be loaded"
            (retry)="page.reload()" />
        } @else {
          <div class="flex flex-col gap-4">
            @for (account of page.sortedAccounts(); track account.id) {
              <app-service-account-card [account]="account" />
            } @empty {
              <app-panel surface="card">
                <app-empty-state
                  i18n-title="Heading of the empty service account list"
                  title="No service accounts"
                  i18n-description="
                    Explains what to do on the empty service account list
                  "
                  description="Create an identity for Codex or another integration, then issue a scoped credential.">
                  <svg emptyStateIcon lucideBot class="h-8 w-8"></svg>
                  @if (page.canCreate()) {
                    <button
                      emptyStateAction
                      app-flat-button
                      type="button"
                      (click)="page.createAccount()">
                      <svg lucidePlus class="h-4 w-4"></svg>
                      <span
                        i18n="
                          Button that opens the create-service-account dialog
                        ">
                        Create service account
                      </span>
                    </button>
                  }
                </app-empty-state>
              </app-panel>
            }
          </div>
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class ServiceAccountsViewComponent {
  protected readonly page = inject(ServiceAccountsPageService);
}
