import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { ListItemComponent } from '@static/components/list/list-item.component';

@Component({
  selector: 'app-user-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CheckboxComponent, AvatarComponent, ListItemComponent],
  template: `
    <app-list-item>
      <ng-content />

      <app-checkbox class="my-auto flex-none"></app-checkbox>

      <div class="flex w-14 flex-none items-center justify-center">
        <app-avatar
          [imageUrl]="user().pictureUrl"
          [name]="user().displayName"
          [size]="26" />
      </div>

      <div
        class="w-[180px] flex-none overflow-hidden text-sm text-ellipsis whitespace-nowrap">
        {{ user().displayName }}
      </div>

      <div
        class="text-foreground/60 flex-1 overflow-hidden text-sm text-ellipsis whitespace-nowrap">
        {{ user().email }}
      </div>

      @if (user().isWorkspaceOwner) {
        <div
          class="bg-primary/10 text-primary mr-1.5 rounded-full px-1.5 py-0.5 text-sm">
          Owner
        </div>
      }
    </app-list-item>
  `,
})
export class UserListItemComponent {
  readonly user = input.required<WorkspaceAppUser>();
}
