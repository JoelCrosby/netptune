import { Component, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { MediaMatcher } from '@angular/cdk/layout';
import {
  selectIsAuthenticated,
  selectCurrentUserDisplayName,
} from './core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { AuthState } from './core/auth/store/auth.reducer';
import { ActionAuthLogout } from './core/auth/store/auth.actions';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnDestroy {
  authenticated$ = this.store.select(selectIsAuthenticated);
  displayName$ = this.store.select(selectCurrentUserDisplayName);

  links = [
    { label: 'Projects', value: ['/projects'] },
    { label: 'Tasks', value: ['/tasks'] },
    { label: 'Users', value: ['/users'] },
    { label: 'Account', value: ['/profile'] },
    { label: 'Workspaces', value: ['/workspaces'] },
  ];

  mobileQuery: MediaQueryList;

  private mobileQueryListener: () => void;

  constructor(
    private store: Store<AuthState>,
    private router: Router,
    changeDetectorRef: ChangeDetectorRef,
    media: MediaMatcher
  ) {
    this.mobileQuery = media.matchMedia('(max-width: 600px)');
    this.mobileQueryListener = () => changeDetectorRef.detectChanges();
    this.mobileQuery.addListener(this.mobileQueryListener);
  }

  ngOnDestroy(): void {
    this.mobileQuery.removeListener(this.mobileQueryListener);
  }

  onLoginClicked = () => this.router.navigate(['/accounts/login']);
  onLogoutClicked = () => this.store.dispatch(new ActionAuthLogout());
  onProfileClicked = () => this.router.navigate(['/accounts/profile']);
}
