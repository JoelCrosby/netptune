import { selectAllUsers } from './../store/users.selectors';
import { Component, OnInit } from '@angular/core';
import { dropIn } from '@core/animations/animations';
import { AppUser } from '@core/models/appuser';
import { Store, select } from '@ngrx/store';
import { AppState } from '@core/core.state';
import { UsernameConverter } from '@core/models/converters/username.converter';
import { loadUsers } from '../store/users.actions';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';

@Component({
  selector: 'app-users',
  templateUrl: './users.index.component.html',
  styleUrls: ['./users.index.component.scss'],
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

  toDisplay(user: AppUser) {
    return UsernameConverter.toDisplay(user);
  }
}
