import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import * as AuthActions from '@core/auth/store/auth.actions';
import { select, Store } from '@ngrx/store';
import { loadProfile } from '@profile/store/profile.actions';
import * as ProfileSelectors from '@profile/store/profile.selectors';
import { Observable } from 'rxjs';

@Component({
  templateUrl: './profile-view.component.html',
  styleUrls: ['./profile-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileViewComponent implements OnInit, AfterViewInit {
  loadingUpdate$!: Observable<boolean>;
  loading$!: Observable<boolean>;

  constructor(private store: Store) {}

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
