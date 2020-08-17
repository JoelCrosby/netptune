import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AppUser } from '@app/core/models/appuser';
import { selectBoardGroupsUsersModel } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-board-group-users',
  templateUrl: './board-group-users.component.html',
  styleUrls: ['./board-group-users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupUsersComponent implements OnInit {
  users$: Observable<
    {
      user: AppUser;
      selected: boolean;
    }[]
  >;

  constructor(private store: Store) {}

  ngOnInit() {
    this.users$ = this.store.select(selectBoardGroupsUsersModel);
  }
}
