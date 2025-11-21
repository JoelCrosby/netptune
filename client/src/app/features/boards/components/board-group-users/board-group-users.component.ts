import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toggleUserSelection } from '@boards/store/groups/board-groups.actions';
import { selectBoardGroupsUsersModel } from '@boards/store/groups/board-groups.selectors';
import { AppUser } from '@core/models/appuser';
import { Selected } from '@core/models/selected';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';

@Component({
  selector: 'app-board-group-users',
  templateUrl: './board-group-users.component.html',
  styleUrls: ['./board-group-users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent],
})
export class BoardGroupUsersComponent {
  private store = inject(Store);

  users = this.store.selectSignal(selectBoardGroupsUsersModel);

  onUserClicked(user: Selected<AppUser>) {
    this.store.dispatch(toggleUserSelection({ user }));
  }

  trackByUsers(_: number, user: Selected<AppUser>) {
    return user.id;
  }
}
