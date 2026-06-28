import { Component, inject } from '@angular/core';
import { toggleUserSelection } from '@app/core/store/groups/board-groups.actions';
import {
  BoardGroupUserModel,
  selectBoardGroupsUsersModel,
} from '@app/core/store/groups/board-groups.selectors';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { AppUser } from '@core/models/appuser';
import { Selected } from '@core/models/selected';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';

@Component({
  selector: 'app-board-group-users',
  template: `
    <div class="inline-flex flex-row-reverse items-center">
      @for (user of users(); track trackByUsers($index, user)) {
        <div
          class="relative inline-flex not-last:-ml-3 hover:z-100"
          [style.z-index]="user.selected ? 99 : null">
          <div
            class="bg-background inline-flex h-10 w-10 cursor-pointer items-center justify-center overflow-hidden rounded-full border-4"
            [class.border-transparent]="!user.selected"
            [class.border-primary]="user.selected">
            <app-avatar
              size="lg"
              [name]="user.displayName"
              [imageUrl]="user.pictureUrl"
              (click)="onUserClicked(user)">
            </app-avatar>
          </div>
          @if (user.online) {
            <span
              class="border-background pointer-events-none absolute right-0.5 bottom-0.5 h-3 w-3 rounded-full border-2 bg-green-500"
              [appTooltip]="user.displayName + ' is viewing this board'">
            </span>
          }
        </div>
      }
    </div>
  `,
  imports: [AvatarComponent, TooltipDirective],
})
export class BoardGroupUsersComponent {
  private store = inject(Store);

  users = this.store.selectSignal(selectBoardGroupsUsersModel);

  onUserClicked(user: Selected<AppUser>) {
    this.store.dispatch(toggleUserSelection({ user }));
  }

  trackByUsers(_: number, user: BoardGroupUserModel) {
    return user.id;
  }
}
