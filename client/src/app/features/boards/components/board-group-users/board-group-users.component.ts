import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toggleUserSelection } from '@boards/store/groups/board-groups.actions';
import { selectBoardGroupsUsersModel } from '@boards/store/groups/board-groups.selectors';
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
          class="bg-background inline-flex h-10 w-10 cursor-pointer items-center justify-center overflow-hidden rounded-full border-2 not-last:-ml-3 hover:z-100"
          [class.border-transparent]="!user.selected"
          [class.border-primary]="user.selected"
          [style.z-index]="user.selected ? 99 : null">
          <app-avatar
            size="lg"
            [name]="user.displayName"
            [imageUrl]="user.pictureUrl"
            (click)="onUserClicked(user)">
          </app-avatar>
        </div>
      }
    </div>
  `,
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
