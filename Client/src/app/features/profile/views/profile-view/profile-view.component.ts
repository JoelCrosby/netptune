import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import * as AuthActions from '@core/auth/store/auth.actions';
import { select, Store } from '@ngrx/store';
import * as ProfileSelectors from '@profile/store/profile.selectors';
import { Observable } from 'rxjs';

@Component({
  templateUrl: './profile-view.component.html',
  styleUrls: ['./profile-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileViewComponent implements OnInit {
  loadingUpdate$: Observable<boolean>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.loadingUpdate$ = this.store.pipe(
      select(ProfileSelectors.selectUpdateProfileLoading)
    );
  }

  onLogoutClicked() {
    this.store.dispatch(AuthActions.logout());
  }
}
