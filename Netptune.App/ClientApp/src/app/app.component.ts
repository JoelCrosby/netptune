import { Component, OnInit, ViewChild } from '@angular/core';
import { MatSidenav } from '@angular/material';
import { Router } from '@angular/router';
import { ActionAuthLogout } from '@core/auth/store/auth.actions';
import { selectCurrentUserDisplayName, selectIsAuthenticated } from '@core/auth/store/auth.selectors';
import { AppState, selectPageTitle } from '@core/core.state';
import { MediaService } from '@core/media/media.service';
import { selectEffectiveTheme } from '@core/settings/settings.selectors';
import { Store, select } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  @ViewChild(MatSidenav) sideNav: MatSidenav;

  theme$: Observable<string>;
  authenticated$: Observable<boolean>;
  displayName$: Observable<string>;
  pageTitle$: Observable<string>;

  links = [
    { label: 'Projects', value: ['/projects'] },
    { label: 'Tasks', value: ['/tasks'] },
    { label: 'Users', value: ['/users'] },
    { label: 'Account', value: ['/profile'] },
    { label: 'Workspaces', value: ['/workspaces'] },
    { label: 'Settings', value: ['/settings'] },
  ];

  mobileQuery: MediaQueryList;

  constructor(private store: Store<AppState>, private router: Router, private mediaService: MediaService) {
    this.mobileQuery = this.mediaService.mobileQuery;
  }

  ngOnInit() {
    this.theme$ = this.store.pipe(select(selectEffectiveTheme));
    this.authenticated$ = this.store.pipe(select(selectIsAuthenticated));
    this.displayName$ = this.store.pipe(select(selectCurrentUserDisplayName));
    this.pageTitle$ = this.store.pipe(select(selectPageTitle));
  }

  onToggleSideNav = () => this.sideNav.toggle();
  onLoginClicked = () => this.router.navigate(['/accounts/login']);
  onLogoutClicked = () => this.store.dispatch(new ActionAuthLogout());
  onProfileClicked = () => this.router.navigate(['/profile']);
}
