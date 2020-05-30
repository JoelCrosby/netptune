import { selectAllUsers } from '@users/store/users.selectors';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { dropIn } from '@core/animations/animations';
import { AppUser } from '@core/models/appuser';
import { Store, select } from '@ngrx/store';
import { AppState } from '@core/core.state';
import { loadUsers } from '@users/store/users.actions';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';

@Component({
  selector: 'app-users',
  templateUrl: './users.index.component.html',
  styleUrls: ['./users.index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [dropIn],
})
export class UsersComponent implements OnInit {
  users$ = this.store.pipe(select(selectAllUsers));

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store<AppState>
  ) {}

  ngOnInit() {
    this.store.dispatch(loadUsers());
  }

  trackById(index: number, user: AppUser) {
    return user.id;
  }
}
