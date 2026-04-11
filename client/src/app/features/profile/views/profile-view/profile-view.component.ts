import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { logout } from '@core/auth/store/auth.actions';
import { Store } from '@ngrx/store';
import { ChangePasswordComponent } from '@profile/components/change-password/change-password.component';
import { UpdateProfileComponent } from '@profile/components/update-profile/update-profile.component';
import { loadProfile } from '@profile/store/profile.actions';
import {
  selectProfileLoading,
  selectUpdateProfileLoading,
} from '@profile/store/profile.selectors';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    UpdateProfileComponent,
    ChangePasswordComponent,
  ],
  template: `<app-page-container
    [showProgress]="loadingUpdate()"
    [centerPage]="true"
    [marginBottom]="true">
    <app-page-header
      title="Profile"
      actionTitle="Logout"
      (actionClick)="onLogoutClicked()" />

    @if (loading()) {
      <div class="flex h-full flex-col items-center justify-center">
        <app-spinner diameter="32px" />
      </div>
    } @else {
      <app-update-profile />
      <div class="border-border my-8 border-b-2"></div>
      <app-change-password />
    }
  </app-page-container> `,
})
export class ProfileViewComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectProfileLoading);
  loadingUpdate = this.store.selectSignal(selectUpdateProfileLoading);

  constructor() {
    this.store.dispatch(loadProfile());
  }

  onLogoutClicked() {
    this.store.dispatch(logout());
  }
}
