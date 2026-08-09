import { Component, computed, inject, output } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { hasPermission } from '@core/auth/has-permission';
import {
  selectCurrentUser,
  selectIsAuthenticated,
} from '@app/core/store/auth/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { currentSprintsResource } from '@core/resources/sprint.resource';
import {
  LucideArchive,
  LucideBell,
  LucideCalendarDays,
  LucideCalendarRange,
  LucideChartNoAxesColumn,
  LucideChartGantt,
  LucideChartSpline,
  LucideGitFork,
  LucideLayoutDashboard,
  LucideLayoutGrid,
  LucideListChecks,
  LucideLogs,
  LucideHardDrive,
  LucideSettings,
  LucideSettings2,
  LucideShield,
  LucideSlidersHorizontal,
  LucideSparkles,
  LucideBot,
  LucideDatabase,
  LucideSquareCheckBig,
  LucideTable2,
  LucideTag,
  LucideUsers,
  LucideWorkflow,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { PERMISSONS } from '../core/auth/permissions';
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
      class="border-side-bar-border bg-side-bar z-10 flex h-full flex-col justify-between overflow-y-auto border-r [transition:width_.2s_ease-in-out]">
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
          <app-shell-menu-link [link]="profileLink">
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

  readonly profileLink: ShellMenuLink = {
    label: $localize`:Sidebar link to the signed-in user's profile:Profile`,
    value: ['./profile'],
  };

  private readonly currentSprintsRef = currentSprintsResource();

  currentSprints = this.currentSprintsRef.value;
  currentSprintsLoaded = computed(() => !this.currentSprintsRef.isLoading());

  authenticated = this.store.selectSignal(selectIsAuthenticated);

  isAssistantAvailable = inject(SessionService).isAssistantAvailable;

  canReadMembers = hasPermission(PERMISSONS.members.read);
  canReadWorkspace = hasPermission(PERMISSONS.workspace.read);
  canReadTags = hasPermission(PERMISSONS.tags.read);
  canReadStatuses = hasPermission(PERMISSONS.statuses.read);
  canReadRelationTypes = hasPermission(PERMISSONS.relationTypes.read);

  canReadServiceAccounts = hasPermission(PERMISSONS.serviceAccounts.read);
  canExportData = hasPermission(PERMISSONS.tasks.export);
  canReadAudit = hasPermission(PERMISSONS.audit.read);
  canReadStorage = hasPermission(PERMISSONS.storage.read);
  canReadSprints = hasPermission(PERMISSONS.sprints.read);
  canReadAutomations = hasPermission(PERMISSONS.automations.read);
  canRestoreTasks = hasPermission(PERMISSONS.tasks.restore);
  canReadAssistantConversations = hasPermission(
    PERMISSONS.assistant.readAllConversations
  );

  links = computed(() => {
    const links: ShellMenuLink[] = [];

    if (this.authenticated()) {
      links.push({
        label: $localize`:Sidebar link to the workspace dashboard:Dashboard`,
        value: ['./dashboard'],
        icon: LucideLayoutDashboard,
      });
    }

    links.push(
      {
        label: $localize`:Sidebar link to the task list:Tasks`,
        value: ['./tasks'],
        icon: LucideSquareCheckBig,
        children: this.canRestoreTasks()
          ? [
              {
                label: $localize`:Sidebar link to the archived task list:Archive`,
                value: ['./tasks/archive'],
                icon: LucideArchive,
              },
            ]
          : undefined,
      },
      {
        label: $localize`:Sidebar link to the kanban board list:Boards`,
        value: ['./boards'],
        icon: LucideTable2,
      }
    );

    if (this.canReadSprints()) {
      const activeSprints = this.currentSprints()
        .slice(0, maxSprintLinks)
        .map((sprint) => ({
          label: sprint.name,
          value: ['./sprints', String(sprint.id)],
          icon: LucideCalendarRange,
        }));

      links.push({
        label: $localize`:Sidebar link to the sprint list:Sprints`,
        value: ['./sprints'],
        icon: LucideCalendarRange,
        children: [
          {
            label: $localize`:Sidebar link to the sprint backlog:Backlog`,
            value: ['./sprints/backlog'],
            icon: LucideLogs,
          },
          ...activeSprints,
        ],
      });
    }

    links.push(
      {
        label: $localize`:Sidebar link to the project list:Projects`,
        value: ['./projects'],
        icon: LucideChartNoAxesColumn,
      },
      {
        label: $localize`:Sidebar link to the roadmap timeline:Roadmap`,
        value: ['./roadmap'],
        icon: LucideChartGantt,
      },
      {
        label: $localize`:Sidebar link to the calendar:Calendar`,
        value: ['./calendar'],
        icon: LucideCalendarDays,
      },
      {
        label: $localize`:Sidebar link to the reporting views:Reports`,
        value: ['./reports'],
        icon: LucideChartSpline,
      }
    );

    if (this.canReadMembers()) {
      links.push({
        label: $localize`:Sidebar link to the workspace member list:Users`,
        value: ['./users'],
        icon: LucideUsers,
      });
    }

    if (this.canReadAutomations()) {
      links.push({
        label: $localize`:Sidebar link to the workspace automation rules:Automations`,
        value: ['./automations'],
        icon: LucideWorkflow,
      });
    }

    return links;
  });

  private workspaceSettingsLinks = computed(() => {
    if (!this.authenticated()) return [];

    const links: ShellMenuLink[] = [];

    if (this.canReadWorkspace()) {
      links.push({
        label: $localize`:Sidebar link to general workspace settings:General`,
        value: ['./settings/workspace/general'],
        icon: LucideLayoutGrid,
      });
    }

    if (this.canReadTags()) {
      links.push({
        label: $localize`:Sidebar link to workspace tag settings:Tags`,
        value: ['./settings/workspace/tags'],
        icon: LucideTag,
      });
    }

    if (this.canReadStatuses()) {
      links.push({
        label: $localize`:Sidebar link to workspace task status settings:Statuses`,
        value: ['./settings/workspace/statuses'],
        icon: LucideListChecks,
      });
    }

    if (this.canReadRelationTypes()) {
      links.push({
        label: $localize`:Sidebar link to workspace task relation type settings:Relations`,
        value: ['./settings/workspace/relations'],
        icon: LucideGitFork,
      });
    }

    if (this.canReadServiceAccounts()) {
      links.push({
        label: $localize`:Sidebar link to workspace service account settings:Service Accounts`,
        value: ['./settings/workspace/service-accounts'],
        icon: LucideBot,
      });
    }

    if (this.canReadAssistantConversations()) {
      links.push({
        label: $localize`:Sidebar link to workspace assistant conversation settings:Assistant`,
        value: ['./settings/workspace/assistant'],
        icon: LucideSparkles,
      });
    }

    if (this.canExportData()) {
      links.push({
        label: $localize`:Sidebar link to workspace import and export settings:Data`,
        value: ['./settings/workspace/data'],
        icon: LucideDatabase,
      });
    }

    return links;
  });

  private personalSettingsLinks = computed(() => {
    if (!this.authenticated()) return [];

    const links: ShellMenuLink[] = [
      {
        label: $localize`:Sidebar link to general personal settings:General`,
        value: ['./settings/personal/general'],
        icon: LucideSlidersHorizontal,
      },
      {
        label: $localize`:Sidebar link to personal notification settings:Notifications`,
        value: ['./settings/personal/notifications'],
        icon: LucideBell,
      },
    ];

    if (this.isAssistantAvailable()) {
      links.push({
        label: $localize`:Sidebar link to the personal assistant key settings:Assistant`,
        value: ['./settings/personal/assistant'],
        icon: LucideSparkles,
      });
    }

    return links;
  });

  bottomLinks = computed(() => {
    const links: ShellMenuLink[] = [];

    if (this.authenticated()) {
      links.push({
        label: $localize`:Sidebar link to the notification list:Notifications`,
        value: ['./notifications'],
        icon: LucideBell,
      });
    }

    if (this.canReadStorage()) {
      links.push({
        label: $localize`:Sidebar link to uploaded file storage:Storage`,
        value: ['./storage'],
        icon: LucideHardDrive,
      });
    }

    if (this.canReadAudit()) {
      links.push({
        label: $localize`:Sidebar link to the workspace audit log:Audit Log`,
        value: ['./audit'],
        icon: LucideShield,
      });
    }

    const [defaultWorkspaceSettingsLink, ...workspaceSettingsChildren] =
      this.workspaceSettingsLinks();

    if (defaultWorkspaceSettingsLink) {
      links.push({
        label: $localize`:Sidebar link to workspace settings:Workspace`,
        value: defaultWorkspaceSettingsLink.value,
        icon: LucideSettings2,
        overviewLabel: defaultWorkspaceSettingsLink.label,
        overviewIcon: defaultWorkspaceSettingsLink.icon,
        children: workspaceSettingsChildren,
      });
    }

    const [defaultPersonalSettingsLink, ...personalSettingsChildren] =
      this.personalSettingsLinks();

    if (defaultPersonalSettingsLink) {
      links.push({
        label: $localize`:Sidebar link to personal settings:Settings`,
        value: defaultPersonalSettingsLink.value,
        icon: LucideSettings,
        overviewLabel: defaultPersonalSettingsLink.label,
        overviewIcon: defaultPersonalSettingsLink.icon,
        children: personalSettingsChildren,
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
