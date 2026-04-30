import { netptunePermissions } from '@core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { ListLinkItemComponent } from '@static/components/list/list-link-item.component';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';

@Component({
  selector: 'app-user-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CheckboxComponent, AvatarComponent, ListLinkItemComponent],
  template: `
    <app-list-link-item [link]="routerLink()">
      <ng-content />

      <app-checkbox class="my-auto flex-none"></app-checkbox>

      <div class="flex w-14 flex-none items-center justify-center">
        <app-avatar
          [imageUrl]="user().pictureUrl"
          [name]="user().displayName"
          size="sm" />
      </div>

      <div
        class="w-45 flex-none overflow-hidden text-sm text-ellipsis whitespace-nowrap">
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
    </app-list-link-item>
  `,
})
export class UserListItemComponent {
  readonly user = input.required<WorkspaceAppUser>();
  private store = inject(Store);

  canReadUsers = this.store.selectSignal(
    selectHasPermission(netptunePermissions.members.read)
  );

  routerLink = computed(() => {
    return this.canReadUsers() ? ['.', this.user().id] : null;
  });
}
