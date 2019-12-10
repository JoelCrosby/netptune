import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  ViewChild,
} from '@angular/core';
import { MatSidenav } from '@angular/material';
import { selectIsAuthenticated } from '@core/auth/store/auth.selectors';
import { AppState } from '@core/core.state';
import { MediaService } from '@core/media/media.service';
import { selectAllWorkspaces } from '@core/workspaces/workspaces.selectors';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';

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

  constructor(
    private store: Store<AppState>,
    private mediaService: MediaService
  ) {
    this.mobileQuery = this.mediaService.mobileQuery;
  }

  ngOnInit() {
    this.authenticated$ = this.store.pipe(select(selectIsAuthenticated));
  }
}
