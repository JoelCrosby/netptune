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
      [onlineLabel]="viewingLabel"
      (optionClicked)="onUserClicked($event)" />
  `,
})
export class BoardGroupUsersComponent {
  private store = inject(Store);

  /**
   * Angular disallows `i18n-onlineLabel` because the attribute name starts with
   * "on" and is treated as an event property, so the copy is localised here and
   * passed as a binding.
   */
  readonly viewingLabel = $localize`:Presence state appended after a person's name on a board:is viewing this board`;

  users = this.store.selectSignal(selectBoardGroupsUsersModel);

  onUserClicked(option: AvatarFilterOption) {
    const user = this.users().find((item) => item.id === option.id);

    if (user) {
      this.store.dispatch(toggleUserSelection({ user }));
    }
  }
}
