import { Component, OnInit, ViewChild } from '@angular/core';
import { MatSidenav } from '@angular/material';
import { Router } from '@angular/router';
import { logout } from '@core/auth/store/auth.actions';
import {
  selectCurrentUserDisplayName,
  selectIsAuthenticated,
} from '@core/auth/store/auth.selectors';
import { AppState, selectPageTitle } from '@core/core.state';
import { MediaService } from '@core/media/media.service';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss'],
})
export class ShellComponent implements OnInit {
  @ViewChild(MatSidenav) sideNav: MatSidenav;

  authenticated$: Observable<boolean>;
  displayName$: Observable<string>;
  pageTitle$: Observable<string>;

  links = [
    { label: 'Projects', value: ['./projects'], icon: 'assessment' },
    { label: 'Tasks', value: ['./tasks'], icon: 'check_box' },
    { label: 'Boards', value: ['./boards'], icon: 'table_chart' },
    { label: 'Users', value: ['./users'], icon: 'supervised_user_circle' },
    { label: 'Settings', value: ['./settings'], icon: 'settings_applications' },
  ];

  mobileQuery: MediaQueryList;

  constructor(
    private store: Store<AppState>,
    private router: Router,
    private mediaService: MediaService
  ) {
    this.mobileQuery = this.mediaService.mobileQuery;
  }

  ngOnInit() {
    this.authenticated$ = this.store.pipe(select(selectIsAuthenticated));
    this.displayName$ = this.store.pipe(select(selectCurrentUserDisplayName));
    this.pageTitle$ = this.store.pipe(select(selectPageTitle));
  }

  onToggleSideNav = () => this.sideNav.toggle();
  onLoginClicked = () => this.router.navigate(['/accounts/login']);
  onLogoutClicked = () => this.store.dispatch(logout());
}
