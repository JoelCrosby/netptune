import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { dropIn } from '@core/animations/animations';
import { AppUser } from '@core/models/appuser';
import { Store } from '@ngrx/store';
import { AppState } from '@core/core.state';
import { ActionLoadUsers } from '../store/users.actions';
import { UsernameConverter } from '@core/models/converters/username.converter';
import { selectUsers } from '../store/users.selectors';

@Component({
  selector: 'app-users',
  templateUrl: './users.index.component.html',
  styleUrls: ['./users.index.component.scss'],
  animations: [dropIn],
})
export class UsersComponent implements OnInit {
  users$ = this.store.select(selectUsers);

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store<AppState>
  ) {}

  ngOnInit() {
    this.store.dispatch(new ActionLoadUsers());
  }

  trackById(index: number, user: AppUser) {
    return user.id;
  }

  toDisplay(user: AppUser) {
    return UsernameConverter.toDisplay(user);
  }
}
