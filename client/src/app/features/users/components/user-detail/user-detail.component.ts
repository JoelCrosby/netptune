import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PermissionListComponent } from '@app/static/components/permission-list/permission-list.component';
import { selectUserDetail } from '@core/store/users/users.selectors';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';

@Component({
  selector: 'app-user-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, PermissionListComponent],
  template: ` @if (user(); as user) {
      <div class="flex flex-col items-baseline gap-8 pb-96">
        <app-avatar
          [name]="user.displayName"
          [imageUrl]="user.pictureUrl"
          size="xl" />
        <p
          class="text-foreground/70 bg-secondary-background rounded-sm px-4 py-1 text-lg font-medium">
          {{ user.email }}
        </p>

        <h2 class="text-foreground pl-1 text-2xl">Permissions</h2>

        <div class="w-full">
          <app-permission-list />
        </div>
      </div>
    } @else {
      <p>User not found</p>
    }`,
})
export class UserDetailComponent {
  readonly store = inject(Store);
  user = this.store.selectSignal(selectUserDetail);
}
