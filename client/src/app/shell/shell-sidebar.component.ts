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
} from '@app/core/store/auth/auth.selectors';
import { Workspace } from '@core/models/workspace';
import {
  LucideBarChart2,
  LucideCheckSquare,
  LucideCalendarDays,
  LucideSettings,
  LucideShield,
  LucideTable2,
  LucideUsers,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { netptunePermissions } from '../core/auth/permissions';
import { ShellMenuLinkListComponent } from './shell-menu-link-list.component';
import { ShellMenuLinkComponent } from './shell-menu-link.component';
import { ShellSidebarCollapseComponent } from './shell-sidebar-collapse.component';
import { ShellService } from './shell.service';
import { WorkspaceSelectComponent } from './workspace-select/workspace-select.component';

@Component({
  selector: 'app-shell-sidebar',
  template: `
    <div
      class="border-side-bar-border bg-side-bar z-10 flex h-full flex-col justify-between border-r [transition:width_.2s_ease-in-out]">
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

        @if (user(); as user) {
          <app-shell-menu-link
            [link]="{ label: 'Profile', value: ['./profile'] }">
            <app-avatar
              class="app-menu-link-profile"
              [name]="user.displayName"
              [imageUrl]="user.pictureUrl"
              size="sm"
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

  canReadAudit = this.store.selectSignal(
    selectHasPermission(netptunePermissions.audit.read)
  );

  links = computed(() => {
    const links = [];

    if (this.canReadMembers()) {
      links.push({ label: 'Users', value: ['./users'], icon: LucideUsers });
    }

    return [
      { label: 'Projects', value: ['./projects'], icon: LucideBarChart2 },
      { label: 'Boards', value: ['./boards'], icon: LucideTable2 },
      { label: 'Sprints', value: ['./sprints'], icon: LucideCalendarDays },
      {
        label: 'Tasks',
        value: ['./tasks'],
        icon: LucideCheckSquare,
      },
      ...links,
    ];
  });

  bottomLinks = computed(() => {
    const links = [];

    if (this.canReadAudit()) {
      links.push({
        label: 'Audit Log',
        value: ['./audit'],
        icon: LucideShield,
      });
    }

    if (this.canReadWorkspace()) {
      links.push({
        label: 'Settings',
        value: ['./settings'],
        icon: LucideSettings,
      });
    }

    return links;
  });

  user = this.store.selectSignal(selectCurrentUser);
  workspaceChange = output<Workspace>();

  onWorkspaceChange(workspace: Workspace) {
    this.workspaceChange.emit(workspace);
  }
}
