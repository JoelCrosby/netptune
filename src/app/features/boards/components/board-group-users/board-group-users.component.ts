import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AppUser } from '@app/core/models/appuser';
import { toggleUserSelection } from '@boards/store/groups/board-groups.actions';
import { selectBoardGroupsUsersModel } from '@boards/store/groups/board-groups.selectors';
import { Selected } from '@core/models/selected';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-board-group-users',
  templateUrl: './board-group-users.component.html',
  styleUrls: ['./board-group-users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupUsersComponent implements OnInit {
  users$: Observable<Selected<AppUser>[]>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.users$ = this.store.select(selectBoardGroupsUsersModel);
  }

  onUserClicked(user: Selected<AppUser>) {
    this.store.dispatch(toggleUserSelection({ user: user.item }));
  }
}
