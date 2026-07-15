import { Component, computed, effect, inject, output } from '@angular/core';
import {
  selectCurrentUser,
  selectHasPermission,
} from '@app/core/store/auth/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { loadCurrentSprints } from '@core/store/sprints/sprints.actions';
import {
  selectCurrentSprints,
  selectCurrentSprintsLoaded,
} from '@core/store/sprints/sprints.selectors';
import {
  LucideArchive,
  LucideBell,
  LucideCalendarDays,
  LucideCalendarRange,
  LucideChartNoAxesColumn,
  LucideGitFork,
  LucideLayoutDashboard,
  LucideLayoutGrid,
  LucideListChecks,
  LucideLogs,
  LucideHardDrive,
  LucideSettings,
  LucideSettings2,
  LucideShield,
  LucideSquareCheckBig,
  LucideTable2,
  LucideTag,
  LucideUsers,
  LucideWorkflow,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { netptunePermissions } from '../core/auth/permissions';
import { ShellMenuLinkListComponent } from './shell-menu-link-list.component';
import {
  ShellMenuLink,
  ShellMenuLinkComponent,
} from './shell-menu-link.component';
import { ShellSidebarCollapseComponent } from './shell-sidebar-collapse.component';
import { ShellService } from './shell.service';
import { WorkspaceSelectComponent } from './workspace-select/workspace-select.component';

/** Active sprints listed under the Sprints menu. Overview links to the full list. */
const maxSprintLinks = 2;

@Component({
  selector: 'app-shell-sidebar',
  template: `
    <div
      class="border-side-bar-border bg-side-bar z-10 flex h-full flex-col justify-between border-r [transition:width_.2s_ease-in-out]">
      <app-workspace-select
        idKey="id"
        labelKey="name"
        (selectChange)="onWorkspaceChange($event)" />

      <app-shell-menu-link-list>
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

  currentSprints = this.store.selectSignal(selectCurrentSprints);
  currentSprintsLoaded = this.store.selectSignal(selectCurrentSprintsLoaded);

  canReadMembers = this.store.selectSignal(
    selectHasPermission(netptunePermissions.members.read)
  );

  canReadWorkspace = this.store.selectSignal(
    selectHasPermission(netptunePermissions.workspace.read)
  );

  canReadTags = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tags.read)
  );

  canReadStatuses = this.store.selectSignal(
    selectHasPermission(netptunePermissions.statuses.read)
  );

  canReadRelationTypes = this.store.selectSignal(
    selectHasPermission(netptunePermissions.relationTypes.read)
  );

  canReadAudit = this.store.selectSignal(
    selectHasPermission(netptunePermissions.audit.read)
  );

  canReadStorage = this.store.selectSignal(
    selectHasPermission(netptunePermissions.storage.read)
  );

  canReadSprints = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.read)
  );

  canReadAutomations = this.store.selectSignal(
    selectHasPermission(netptunePermissions.automations.read)
  );

  canRestoreTasks = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.restore)
  );

  links = computed(() => {
    const links = [];

    if (this.canReadMembers()) {
      links.push({ label: 'Users', value: ['./users'], icon: LucideUsers });
    }

    const primaryLinks: ShellMenuLink[] = [
      {
        label: 'Dashboard',
        value: ['./dashboard'],
        icon: LucideLayoutDashboard,
      },
      {
        label: 'Projects',
        value: ['./projects'],
        icon: LucideChartNoAxesColumn,
      },
      { label: 'Boards', value: ['./boards'], icon: LucideTable2 },
      {
        label: 'Tasks',
        value: ['./tasks'],
        icon: LucideSquareCheckBig,
        children: this.canRestoreTasks()
          ? [
              {
                label: 'Archive',
                value: ['./tasks/archive'],
                icon: LucideArchive,
              },
            ]
          : undefined,
      },
    ];

    if (this.canReadAutomations()) {
      primaryLinks.push({
        label: 'Automations',
        value: ['./automations'],
        icon: LucideWorkflow,
      });
    }

    if (this.canReadSprints()) {
      const activeSprints = this.currentSprints()
        .slice(0, maxSprintLinks)
        .map((sprint) => ({
          label: sprint.name,
          value: ['./sprints', String(sprint.id)],
          icon: LucideCalendarRange,
        }));

      primaryLinks.splice(2, 0, {
        label: 'Sprints',
        value: ['./sprints'],
        icon: LucideCalendarDays,
        children: [
          {
            label: 'Backlog',
            value: ['./sprints/backlog'],
            icon: LucideLogs,
          },
          ...activeSprints,
        ],
      });
    }

    return [...primaryLinks, ...links];
  });

  bottomLinks = computed(() => {
    const links: ShellMenuLink[] = [];

    if (this.canReadAudit()) {
      links.push({
        label: 'Audit Log',
        value: ['./audit'],
        icon: LucideShield,
      });
    }

    if (this.canReadStorage()) {
      links.push({
        label: 'Storage',
        value: ['./storage'],
        icon: LucideHardDrive,
      });
    }

    const workspaceSettingsLinks: ShellMenuLink[] = [];

    if (this.canReadWorkspace()) {
      workspaceSettingsLinks.push({
        label: 'General',
        value: ['./settings/workspace/general'],
        icon: LucideLayoutGrid,
      });
    }

    if (this.canReadTags()) {
      workspaceSettingsLinks.push({
        label: 'Tags',
        value: ['./settings/workspace/tags'],
        icon: LucideTag,
      });
    }

    if (this.canReadStatuses()) {
      workspaceSettingsLinks.push({
        label: 'Statuses',
        value: ['./settings/workspace/statuses'],
        icon: LucideListChecks,
      });
    }

    if (this.canReadRelationTypes()) {
      workspaceSettingsLinks.push({
        label: 'Relations',
        value: ['./settings/workspace/relations'],
        icon: LucideGitFork,
      });
    }

    const [defaultWorkspaceSettingsLink, ...workspaceSettingsChildren] =
      workspaceSettingsLinks;

    if (defaultWorkspaceSettingsLink) {
      links.push({
        label: 'Workspace',
        value: defaultWorkspaceSettingsLink.value,
        icon: LucideSettings2,
        overviewLabel: defaultWorkspaceSettingsLink.label,
        overviewIcon: defaultWorkspaceSettingsLink.icon,
        children: workspaceSettingsChildren,
      });
    }

    links.push({
      label: 'Settings',
      value: ['./settings/personal'],
      icon: LucideSettings,
    });

    links.push({
      label: 'Notifications',
      value: ['./notifications'],
      icon: LucideBell,
    });

    return links;
  });

  user = this.store.selectSignal(selectCurrentUser);
  workspaceChange = output<Workspace>();

  constructor() {
    effect(() => {
      if (this.canReadSprints() && !this.currentSprintsLoaded()) {
        this.store.dispatch(loadCurrentSprints.init());
      }
    });
  }

  onWorkspaceChange(workspace: Workspace) {
    this.workspaceChange.emit(workspace);
  }
}
