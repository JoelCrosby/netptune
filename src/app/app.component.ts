import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  ViewChild,
} from '@angular/core';
import { MatSidenav } from '@angular/material/sidenav';
import { selectIsAuthenticated } from '@core/auth/store/auth.selectors';
import { MediaService } from '@core/media/media.service';
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

  authenticated$: Observable<boolean>;
  workspaces$ = this.store.select(selectAllWorkspaces);

  mobileQuery: MediaQueryList;

  constructor(private store: Store, private mediaService: MediaService) {
    this.mobileQuery = this.mediaService.mobileQuery;
  }

  ngOnInit(): void {
    this.authenticated$ = this.store.pipe(
      select(selectIsAuthenticated),
      tap((value) => value && this.store.dispatch(loadWorkspaces()))
    );
  }
}
