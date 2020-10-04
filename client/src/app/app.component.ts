import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  ViewChild,
} from '@angular/core';
import { MatSidenav } from '@angular/material/sidenav';
import * as AuthSelectors from '@core/auth/store/auth.selectors';
import * as LayoutSelectors from '@core/store/layout/layout.selectors';
import { loadWorkspaces } from '@core/store/workspaces/workspaces.actions';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { map, tap, withLatestFrom } from 'rxjs/operators';
import { selectSideBarTransparent } from '@core/core.route.selectors';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent implements OnInit {
  @ViewChild(MatSidenav) sideNav: MatSidenav;

  sideNavOpen$: Observable<boolean>;

  workspaces$ = this.store.select(selectAllWorkspaces);
  isMobileView$ = this.store.select(LayoutSelectors.selectIsMobileView);
  transparentSideNav$ = this.store.select(selectSideBarTransparent);
  sideMenuOpen$ = this.store.select(LayoutSelectors.selectSideMenuOpen);
  user$ = this.store.select(AuthSelectors.selectCurrentUser);

  constructor(private store: Store) {}

  ngOnInit() {
    this.sideNavOpen$ = this.store
      .select(LayoutSelectors.selectSideNavOpen)
      .pipe(
        withLatestFrom(this.store.select(AuthSelectors.selectIsAuthenticated)),
        tap(([_, isAuth]) => isAuth && this.store.dispatch(loadWorkspaces())),
        map(([isNavOpen, isAuth]) => isAuth && isNavOpen)
      );
  }
}
