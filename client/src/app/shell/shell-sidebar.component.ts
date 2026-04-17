import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  output,
} from '@angular/core';
import {
  selectCurrentUser,
  selectHasPermission,
} from '@core/auth/store/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { Store } from '@ngrx/store';
import {
  LucideBarChart2,
  LucideCheckSquare,
  LucideSettings,
  LucideTable2,
  LucideUsers,
} from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { ShellMenuLinkListComponent } from './shell-menu-link-list.component';
import { ShellMenuLinkComponent } from './shell-menu-link.component';
import { ShellSidebarCollapseComponent } from './shell-sidebar-collapse.component';
import { ShellService } from './shell.service';
import { WorkspaceSelectComponent } from './workspace-select/workspace-select.component';
import { netptunePermissions } from '../core/auth/permissions';
import { NotificationBellComponent } from '@entry/components/notification-bell/notification-bell.component';

@Component({
  selector: 'app-shell-sidebar',
  template: `
    <div
      class="border-side-bar-border bg-side-bar fixed top-0 flex h-screen w-18 flex-col justify-between border-r [transition:width_.2s_ease-in-out]"
      [class.w-[248px]]="shell.sideNavExpanded()">
      <app-shell-menu-link-list>
        <app-workspace-select
          idKey="id"
          labelKey="name"
          (selectChange)="onWorkspaceChange($event)">
        </app-workspace-select>
        @for (link of links(); track link.value) {
          <app-shell-menu-link [link]="link" />
        }
      </app-shell-menu-link-list>

      <div class="flex-1"></div>

      <app-shell-menu-link-list>
        @for (link of bottomLinks(); track link.value) {
          <app-shell-menu-link [link]="link" />
        }
        <div class="flex justify-center py-2">
          <app-notification-bell />
        </div>
        @if (user(); as user) {
          <app-shell-menu-link
            [link]="{ label: 'Profile', value: ['./profile'] }">
            <app-avatar
              class="app-menu-link-profile"
              [name]="user.displayName"
              [imageUrl]="user.pictureUrl"
              [size]="24"
              [border]="true"
              [tooltip]="false" />
          </app-shell-menu-link>
        }
      </app-shell-menu-link-list>
      <app-shell-sidebar-collapse />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    WorkspaceSelectComponent,
    AvatarComponent,
    ShellMenuLinkComponent,
    ShellMenuLinkListComponent,
    ShellSidebarCollapseComponent,
    NotificationBellComponent,
  ],
})
export class ShellSidebarComponent {
  private store = inject(Store);

  shell = inject(ShellService);

  canReadMembers = this.store.selectSignal(
    selectHasPermission(netptunePermissions.members.read)
  );

  canReadWorkspace = this.store.selectSignal(
    selectHasPermission(netptunePermissions.workspace.read)
  );

  links = computed(() => {
    const links = [];

    if (this.canReadMembers()) {
      links.push({ label: 'Users', value: ['./users'], icon: LucideUsers });
    }

    return [
      { label: 'Projects', value: ['./projects'], icon: LucideBarChart2 },
      { label: 'Boards', value: ['./boards'], icon: LucideTable2 },
      {
        label: 'Tasks',
        value: ['./tasks'],
        icon: LucideCheckSquare,
      },
      ...links,
    ];
  });

  bottomLinks = computed(() => {
    if (this.canReadWorkspace()) {
      return [
        { label: 'Settings', value: ['./settings'], icon: LucideSettings },
      ];
    }

    return [];
  });

  user = this.store.selectSignal(selectCurrentUser);

  workspaceChange = output<Workspace>();

  onWorkspaceChange(workspace: Workspace) {
    this.workspaceChange.emit(workspace);
  }
}
