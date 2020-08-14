import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  ViewChild,
} from '@angular/core';
import { MatSidenav } from '@angular/material/sidenav';
import {
  selectIsAuthenticated,
  selectCurrentUser,
} from '@core/auth/store/auth.selectors';
import {
  selectSideMenuOpen,
  selectIsMobileView,
} from '@core/store/layout/layout.selectors';
import { loadWorkspaces } from '@core/store/workspaces/workspaces.actions';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap, withLatestFrom, map } from 'rxjs/operators';

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
  isMobileView$ = this.store.select(selectIsMobileView);
  user$ = this.store.select(selectCurrentUser);

  constructor(private store: Store) {}

  ngOnInit() {
    this.sideNavOpen$ = this.store.pipe(
      select(selectIsAuthenticated),
      withLatestFrom(this.store.select(selectSideMenuOpen)),
      tap(
        ([authenticated]) =>
          authenticated && this.store.dispatch(loadWorkspaces())
      ),
      map(([authenticated, sideNavOpen]) => authenticated && sideNavOpen)
    );
  }
}
