import { Component, OnDestroy, ChangeDetectorRef, ElementRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { MediaMatcher } from '@angular/cdk/layout';
import {
  selectIsAuthenticated,
  selectCurrentUserDisplayName,
} from './core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { ActionAuthLogout } from './core/auth/store/auth.actions';
import { AppState, selectPageTitle } from './core/core.state';
import { selectEffectiveTheme } from './features/settings/store/settings.selectors';
import { MatSidenav } from '@angular/material';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnDestroy {
  @ViewChild(MatSidenav) sideNav: MatSidenav;

  theme$ = this.store.select(selectEffectiveTheme);
  authenticated$ = this.store.select(selectIsAuthenticated);
  displayName$ = this.store.select(selectCurrentUserDisplayName);
  pageTitle$ = this.store.select(selectPageTitle);

  links = [
    { label: 'Projects', value: ['/projects'] },
    { label: 'Tasks', value: ['/tasks'] },
    { label: 'Users', value: ['/users'] },
    { label: 'Account', value: ['/profile'] },
    { label: 'Workspaces', value: ['/workspaces'] },
    { label: 'Settings', value: ['/settings'] },
  ];

  mobileQuery: MediaQueryList;

  private mobileQueryListener: () => void;

  constructor(
    private store: Store<AppState>,
    private router: Router,
    changeDetectorRef: ChangeDetectorRef,
    media: MediaMatcher
  ) {
    this.mobileQuery = media.matchMedia('(max-width: 600px)');
    this.mobileQueryListener = () => changeDetectorRef.detectChanges();
    this.mobileQuery.addListener(this.mobileQueryListener);
  }

  toggleSideNav(): void {
    console.log(this.sideNav);
    this.sideNav.toggle();
  }

  ngOnDestroy(): void {
    this.mobileQuery.removeListener(this.mobileQueryListener);
  }

  onLoginClicked = () => this.router.navigate(['/accounts/login']);
  onLogoutClicked = () => this.store.dispatch(new ActionAuthLogout());
  onProfileClicked = () => this.router.navigate(['/accounts/profile']);
}
