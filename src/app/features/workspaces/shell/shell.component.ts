import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  ViewChild,
} from '@angular/core';
import { MatSidenav } from '@angular/material/sidenav';
import { Router } from '@angular/router';
import {
  selectSideMenuOpen,
  selectSideMenuMode,
} from '@app/core/store/layout/layout.selectors';
import { logout } from '@core/auth/store/auth.actions';
import * as AuthSelectors from '@core/auth/store/auth.selectors';
import { selectPageTitle } from '@core/core.route.selectors';
import { select, Store } from '@ngrx/store';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

@Component({
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
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
  ];

  bottomLinks = [
    { label: 'Profile', value: ['./profile'], icon: 'perm_identity' },
    { label: 'Settings', value: ['./settings'], icon: 'settings_applications' },
  ];

  sideNavOpen$ = this.store.select(selectSideMenuOpen);
  sideNavMode$ = this.store.select(selectSideMenuMode);
  fixedInViewport$ = of(true);

  constructor(private store: Store, private router: Router) {}

  ngOnInit() {
    this.authenticated$ = this.store.pipe(
      select(AuthSelectors.selectIsAuthenticated)
    );
    this.displayName$ = this.store.pipe(
      select(AuthSelectors.selectCurrentUserDisplayName)
    );
    this.pageTitle$ = this.store.pipe(select(selectPageTitle));
  }

  onToggleSideNav = () => this.sideNav.toggle();
  onLoginClicked = () => this.router.navigate(['/accounts/login']);
  onLogoutClicked = () => this.store.dispatch(logout());
}
