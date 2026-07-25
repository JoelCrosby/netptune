import { Component, inject } from '@angular/core';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { Store } from '@ngrx/store';
import { ChangePasswordComponent } from '@profile/components/change-password/change-password.component';
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
  imports: [
    ErrorStateComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    UpdateProfileComponent,
    ChangePasswordComponent,
    LinkedProvidersComponent,
  ],
  template: `<app-page-container
    [showProgress]="loadingUpdate()"
    [centerPage]="true"
    [marginBottom]="true">
    <app-page-header title="Profile" />

    @if (loading()) {
      <app-page-loading />
    } @else if (loadError()) {
      <app-error-state
        title="Your profile could not be loaded"
        description="Check your connection and try again."
        (retry)="reload()" />
    } @else {
      <app-update-profile />
      <div class="border-border my-8 border-b-2"></div>
      <app-change-password />
      <div class="border-border my-8 border-b-2"></div>
      <h2 class="mb-4 text-lg font-semibold">Linked Accounts</h2>
      <app-linked-providers />
    }
  </app-page-container> `,
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
