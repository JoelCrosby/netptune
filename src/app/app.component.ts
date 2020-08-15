import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  ViewChild,
} from '@angular/core';
import { MatSidenav } from '@angular/material/sidenav';
import {
  selectCurrentUser,
  selectIsAuthenticated,
} from '@core/auth/store/auth.selectors';
import { selectIsMobileView } from '@core/store/layout/layout.selectors';
import { loadWorkspaces } from '@core/store/workspaces/workspaces.actions';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

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
      tap(
        (authenticated) =>
          authenticated && this.store.dispatch(loadWorkspaces())
      )
    );
  }
}
