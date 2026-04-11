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

      <div class="flex flex-none w-14 items-center justify-center">
        <app-avatar [imageUrl]="user().pictureUrl" [name]="user().displayName" [size]="26" />
      </div>

      <div class="flex-none w-[180px] overflow-hidden text-ellipsis whitespace-nowrap text-sm">
        {{ user().displayName }}
      </div>

      <div
        class="flex-1 overflow-hidden text-ellipsis whitespace-nowrap text-sm text-foreground/60"
      >
        {{ user().email }}
      </div>

      @if (user().isWorkspaceOwner) {
        <div class="mr-1.5 rounded-full bg-primary/10 px-1.5 py-0.5 text-sm text-primary">
          Owner
        </div>
      }
    </app-list-item>
  `,
})
export class UserListItemComponent {
  readonly user = input.required<WorkspaceAppUser>();
}
