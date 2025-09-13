import { AfterViewInit, ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import * as AuthActions from '@core/auth/store/auth.actions';
import { select, Store } from '@ngrx/store';
import { loadProfile } from '@profile/store/profile.actions';
import * as ProfileSelectors from '@profile/store/profile.selectors';
import { Observable } from 'rxjs';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { AsyncPipe } from '@angular/common';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { UpdateProfileComponent } from '@profile/components/update-profile/update-profile.component';
import { ChangePasswordComponent } from '@profile/components/change-password/change-password.component';

@Component({
  templateUrl: './profile-view.component.html',
  styleUrls: ['./profile-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    UpdateProfileComponent,
    ChangePasswordComponent,
    AsyncPipe
],
})
export class ProfileViewComponent implements OnInit, AfterViewInit {
  private store = inject(Store);

  loadingUpdate$!: Observable<boolean>;
  loading$!: Observable<boolean>;

  ngOnInit() {
    this.loading$ = this.store.select(ProfileSelectors.selectProfileLoading);
    this.loadingUpdate$ = this.store.pipe(
      select(ProfileSelectors.selectUpdateProfileLoading)
    );
  }

  ngAfterViewInit() {
    this.store.dispatch(loadProfile());
  }

  onLogoutClicked() {
    this.store.dispatch(AuthActions.logout());
  }
}
