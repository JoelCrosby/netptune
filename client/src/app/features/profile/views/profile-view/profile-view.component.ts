import { Component, inject } from '@angular/core';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { Store } from '@ngrx/store';
import { AccountPasswordComponent } from '@profile/components/account-password/account-password.component';
import { UpdateProfileComponent } from '@profile/components/update-profile/update-profile.component';
import { LinkedProvidersComponent } from '@profile/components/linked-providers/linked-providers.component';
import { loadProfile } from '@app/core/store/profile/profile.actions';
import {
  selectProfileError,
  selectProfileLoading,
  selectUpdateProfileLoading,
} from '@app/core/store/profile/profile.selectors';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-profile-view',
  imports: [
    ErrorStateComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    UpdateProfileComponent,
    AccountPasswordComponent,
    LinkedProvidersComponent,
  ],
  template: `
    <app-page-container
      [showProgress]="loadingUpdate()"
      [centerPage]="true"
      [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the user profile"
        title="Profile" />

      @if (loading()) {
        <app-page-loading />
      } @else if (loadError()) {
        <app-error-state
          i18n-title="Shown when the profile fails to load"
          title="Your profile could not be loaded"
          i18n-description="Advice shown when a page fails to load"
          description="Check your connection and try again."
          (retry)="reload()" />
      } @else {
        <div class="flex flex-col gap-6">
          <app-update-profile />
          <app-account-password />
          <app-linked-providers />
        </div>
      }
    </app-page-container>
  `,
})
export class ProfileViewComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectProfileLoading);
  loadError = this.store.selectSignal(selectProfileError);
  loadingUpdate = this.store.selectSignal(selectUpdateProfileLoading);

  constructor() {
    this.store.dispatch(loadProfile.init());
  }

  reload() {
    this.store.dispatch(loadProfile.init());
  }
}
