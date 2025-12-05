import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
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
  templateUrl: './profile-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    UpdateProfileComponent,
    ChangePasswordComponent,
  ],
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
