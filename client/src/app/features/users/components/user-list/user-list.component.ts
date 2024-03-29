import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { WorkspaceAppUser } from '@core/models/appuser';
import { removeUsersFromWorkspace } from '@core/store/users/users.actions';
import * as UsersSelectors from '@core/store/users/users.selectors';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserListComponent implements OnInit {
  users$!: Observable<WorkspaceAppUser[]>;

  constructor(
    public snackBar: MatSnackBar,
    public dialog: DialogService,
    private store: Store
  ) {}

  ngOnInit() {
    this.users$ = this.store.pipe(select(UsersSelectors.selectAllUsers));
  }

  trackById(_: number, user: WorkspaceAppUser) {
    return user.id;
  }

  onRemoveClicked(user: WorkspaceAppUser) {
    if (!user) return;

    const emailAddresses = [user.email];
    this.store.dispatch(removeUsersFromWorkspace({ emailAddresses }));
  }
}
