import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AppState } from '@core/core.state';
import { AppUser } from '@core/models/appuser';
import { select, Store } from '@ngrx/store';
import { loadUsers } from '@users/store/users.actions';
import * as UsersSelectors from '@users/store/users.selectors';
import { Observable } from 'rxjs';
import { startWith } from 'rxjs/operators';

@Component({
  selector: 'app-users',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersListComponent implements OnInit, AfterViewInit {
  users$: Observable<AppUser[]>;
  loading$: Observable<boolean>;

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store<AppState>
  ) {}

  ngOnInit() {
    this.users$ = this.store.pipe(select(UsersSelectors.selectAllUsers));
    this.loading$ = this.store
      .select(UsersSelectors.selectUsersLoading)
      .pipe(startWith(true));
  }

  ngAfterViewInit() {
    this.store.dispatch(loadUsers());
  }

  trackById(index: number, user: AppUser) {
    return user.id;
  }
}
