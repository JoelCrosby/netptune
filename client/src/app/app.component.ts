import { ChangeDetectionStrategy, Component, ViewChild } from '@angular/core';
import { MatSidenav } from '@angular/material/sidenav';
import * as AuthSelectors from '@core/auth/store/auth.selectors';
import { selectSideBarTransparent } from '@core/core.route.selectors';
import * as LayoutSelectors from '@core/store/layout/layout.selectors';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { combineLatest } from 'rxjs';
import { map } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  @ViewChild(MatSidenav) sideNav!: MatSidenav;

  sideNavOpen$ = combineLatest([
    this.store.select(LayoutSelectors.selectSideNavOpen),
    this.store.select(AuthSelectors.selectIsAuthenticated),
  ]).pipe(map(([isNavOpen, isAuth]) => isAuth && isNavOpen));

  workspaces$ = this.store.select(selectAllWorkspaces);
  isMobileView$ = this.store.select(LayoutSelectors.selectIsMobileView);
  transparentSideNav$ = this.store.select(selectSideBarTransparent);
  sideMenuOpen$ = this.store.select(LayoutSelectors.selectSideMenuOpen);
  user$ = this.store.select(AuthSelectors.selectCurrentUser);

  constructor(private store: Store) {}
}
