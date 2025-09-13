import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { toggleUserSelection } from '@boards/store/groups/board-groups.actions';
import { selectBoardGroupsUsersModel } from '@boards/store/groups/board-groups.selectors';
import { Selected } from '@core/models/selected';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { AsyncPipe } from '@angular/common';
import { AvatarComponent } from '@static/components/avatar/avatar.component';

@Component({
  selector: 'app-board-group-users',
  templateUrl: './board-group-users.component.html',
  styleUrls: ['./board-group-users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, AsyncPipe],
})
export class BoardGroupUsersComponent implements OnInit {
  private store = inject(Store);

  users$!: Observable<Selected<AppUser>[]>;

  ngOnInit() {
    this.users$ = this.store.select(selectBoardGroupsUsersModel);
  }

  onUserClicked(user: Selected<AppUser>) {
    this.store.dispatch(toggleUserSelection({ user }));
  }

  trackByUsers(_: number, user: Selected<AppUser>) {
    return user.id;
  }
}
