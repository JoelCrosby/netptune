import { Component, inject } from '@angular/core';
import { toggleUserSelection } from '@app/core/store/groups/board-groups.actions';
import { selectBoardGroupsUsersModel } from '@app/core/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import {
  AvatarFilterComponent,
  AvatarFilterOption,
} from '@static/components/avatar-filter/avatar-filter.component';

@Component({
  selector: 'app-board-group-users',
  imports: [AvatarFilterComponent],
  template: `
    <app-avatar-filter
      [options]="users()"
      onlineLabel="is viewing this board"
      (optionClicked)="onUserClicked($event)" />
  `,
})
export class BoardGroupUsersComponent {
  private store = inject(Store);

  users = this.store.selectSignal(selectBoardGroupsUsersModel);

  onUserClicked(option: AvatarFilterOption) {
    const user = this.users().find((item) => item.id === option.id);

    if (user) {
      this.store.dispatch(toggleUserSelection({ user }));
    }
  }
}
