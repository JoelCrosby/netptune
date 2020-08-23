import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AppUser } from '@core/models/appuser';
import { select, Store } from '@ngrx/store';
import {
  loadUsers,
  removeUsersFromWorkspace,
} from '@core/store/users/users.actions';
import * as UsersSelectors from '@core/store/users/users.selectors';
import { Observable } from 'rxjs';
import { startWith } from 'rxjs/operators';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserListComponent implements OnInit, AfterViewInit {
  users$: Observable<AppUser[]>;
  loading$: Observable<boolean>;

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store
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

  onRemoveClicked(user: AppUser) {
    if (!user) return;

    const emailAddresses = [user.email];
    this.store.dispatch(removeUsersFromWorkspace({ emailAddresses }));
  }
}
