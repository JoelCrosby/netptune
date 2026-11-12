import { Injectable, effect, inject, untracked } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { hasPermission } from '@core/auth/has-permission';
import { Router } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';
import { AiPanelService } from '@core/services/ai-panel.service';
import {
  Command,
  injectCommands,
} from '@core/services/command-registry.service';
import { DialogService } from '@core/services/dialog.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { ThemeService, Theme } from '@core/services/theme.service';
import { PageWidthService } from '@core/services/page-width.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import {
  APPEARANCE_PAGE_WIDTH,
  APPEARANCE_THEME,
} from '@core/models/user-preferences';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import {
  TaskDetailLayout,
  TaskDetailLayoutService,
} from '@entry/dialogs/task-detail-dialog/task-detail-layout';
import { CurrentContextService } from '@core/services/current-context.service';
import { PageKind } from '@core/models/pages';
import { PageCommandsService } from '@core/services/page-commands.service';
import { PinCommandsService } from '@core/services/pin-commands.service';
import { KeyboardService } from '@static/services/keyboard.service';

interface AvailableCommand extends Command {
  available?: () => boolean;
}

@Injectable()
export class GlobalCommandsService {
  private router = inject(Router);
  private workspace = inject(WorkspaceService);
  private panel = inject(AiPanelService);
  private dialog = inject(DialogService);
  private theme = inject(ThemeService);
  private pageWidth = inject(PageWidthService);
  private preferences = inject(UserPreferencesService);
  private taskLayout = inject(TaskDetailLayoutService);
  private authenticated = inject(SessionService).isAuthenticated;
  private keyboard = inject(KeyboardService);
  private currentTask = inject(CurrentContextService).task;
  private pinCommands = inject(PinCommandsService);
  private pageCommands = inject(PageCommandsService);

  private canCreateTasks = hasPermission(PERMISSIONS.tasks.create);
  private canRestoreTasks = hasPermission(PERMISSIONS.tasks.restore);
  private canReadSprints = hasPermission(PERMISSIONS.sprints.read);
  private canReadQueries = hasPermission(PERMISSIONS.queries.read);
  private canReadPages = hasPermission(PERMISSIONS.pages.read);
  private canCreatePages = hasPermission(PERMISSIONS.pages.create);
  private canReadMembers = hasPermission(PERMISSIONS.members.read);
  private canReadAutomations = hasPermission(PERMISSIONS.automations.read);
  private canReadCalendar = hasPermission(PERMISSIONS.calendar.read);
  private canReadReports = hasPermission(PERMISSIONS.reports.read);
  private canReadStorage = hasPermission(PERMISSIONS.storage.read);
  private canReadAudit = hasPermission(PERMISSIONS.audit.read);

  private readonly commands: AvailableCommand[] = [
    {
      id: 'nav.dashboard',
      label: $localize`:Command palette action that navigates to the dashboard:Go to Dashboard`,
      group: 'navigation',
      icon: 'layout-dashboard',
      shortcut: ['g', 'd'],
      keywords: ['dashboard', 'home', 'assigned to me', 'navigate'],
      available: () => this.authenticated(),
      execute: () => this.navigate('dashboard'),
    },
    {
      id: 'nav.projects',
      label: $localize`:Command palette action that navigates to the project list:Go to Projects`,
      group: 'navigation',
      icon: 'folder-open',
      shortcut: ['g', 'p'],
      keywords: ['projects', 'navigate'],
      execute: () => this.navigate('projects'),
    },
    {
      id: 'nav.tasks',
      label: $localize`:Command palette action that navigates to the task list:Go to Tasks`,
      group: 'navigation',
      icon: 'hash',
      shortcut: ['g', 't'],
      keywords: ['tasks', 'navigate'],
      execute: () => this.navigate('tasks'),
    },
    {
      id: 'nav.archive',
      label: $localize`:Command palette action that navigates to the archived task list:Go to Archived Tasks`,
      group: 'navigation',
      icon: 'archive',
      keywords: ['archive', 'archived', 'deleted', 'restore', 'tasks'],
      available: () => this.canRestoreTasks(),
      execute: () => this.navigate('tasks/archive'),
    },
    {
      id: 'nav.boards',
      label: $localize`:Command palette action that navigates to the board list:Go to Boards`,
      group: 'navigation',
      icon: 'kanban',
      shortcut: ['g', 'b'],
      keywords: ['boards', 'navigate'],
      execute: () => this.navigate('boards'),
    },
    {
      id: 'nav.sprints',
      label: $localize`:Command palette action that navigates to the sprint list:Go to Sprints`,
      group: 'navigation',
      icon: 'layers',
      shortcut: ['g', 's'],
      keywords: ['sprints', 'navigate'],
      available: () => this.canReadSprints(),
      execute: () => this.navigate('sprints'),
    },
    {
      id: 'nav.backlog',
      label: $localize`:Command palette action that navigates to the sprint backlog:Go to Backlog`,
      group: 'navigation',
      icon: 'logs',
      keywords: ['backlog', 'sprints', 'planning', 'navigate'],
      available: () => this.canReadSprints(),
      execute: () => this.navigate('sprints/backlog'),
    },
    {
      id: 'nav.pages',
      label: $localize`:Command palette action that navigates to the workspace pages:Go to Pages`,
      group: 'navigation',
      icon: 'file-text',
      keywords: ['pages', 'docs', 'documents', 'wiki', 'notes', 'navigate'],
      available: () => this.canReadPages(),
      execute: () => this.navigate('pages'),
    },
    {
      id: 'nav.roadmap',
      label: $localize`:Command palette action that navigates to the roadmap timeline:Go to Roadmap`,
      group: 'navigation',
      icon: 'chart-gantt',
      keywords: ['roadmap', 'timeline', 'gantt', 'navigate'],
      execute: () => this.navigate('roadmap'),
    },
    {
      id: 'nav.calendar',
      label: $localize`:Command palette action that navigates to the calendar:Go to Calendar`,
      group: 'navigation',
      icon: 'calendar-days',
      keywords: ['calendar', 'due dates', 'schedule', 'navigate'],
      available: () => this.canReadCalendar(),
      execute: () => this.navigate('calendar'),
    },
    {
      id: 'nav.reports',
      label: $localize`:Command palette action that navigates to the reporting views:Go to Reports`,
      group: 'navigation',
      icon: 'chart-spline',
      keywords: ['reports', 'reporting', 'charts', 'analytics', 'navigate'],
      available: () => this.canReadReports(),
      execute: () => this.navigate('reports'),
    },
    {
      id: 'nav.queries',
      label: $localize`:Command palette action that navigates to the saved query list:Go to Queries`,
      group: 'navigation',
      icon: 'list-filter',
      keywords: ['queries', 'views', 'filters', 'saved', 'navigate'],
      available: () => this.canReadQueries(),
      execute: () => this.navigate('queries'),
    },
    {
      id: 'nav.pinned',
      label: $localize`:Command palette action that navigates to the pinned task list:Go to Pinned Tasks`,
      group: 'navigation',
      icon: 'pin',
      keywords: ['pinned', 'pins', 'tasks', 'navigate'],
      available: () => this.authenticated(),
      execute: () => this.navigate('pinned'),
    },
    {
      id: 'nav.users',
      label: $localize`:Command palette action that navigates to the member list:Go to Users`,
      group: 'navigation',
      icon: 'users',
      keywords: ['users', 'members', 'navigate'],
      available: () => this.canReadMembers(),
      execute: () => this.navigate('users'),
    },
    {
      id: 'nav.automations',
      label: $localize`:Command palette action that navigates to the automation list:Go to Automations`,
      group: 'navigation',
      icon: 'workflow',
      shortcut: ['g', 'a'],
      keywords: ['automations', 'automation', 'rules', 'workflow'],
      available: () => this.canReadAutomations(),
      execute: () => this.navigate('automations'),
    },
    {
      id: 'nav.notifications',
      label: $localize`:Command palette action that navigates to the notification list:Go to Notifications`,
      group: 'navigation',
      icon: 'bell',
      keywords: ['notifications', 'inbox', 'alerts', 'navigate'],
      available: () => this.authenticated(),
      execute: () => this.navigate('notifications'),
    },
    {
      id: 'nav.assistant',
      label: $localize`:Command palette action that navigates to the full-page assistant chat:Go to Assistant Conversations`,
      group: 'navigation',
      icon: 'sparkles',
      keywords: ['assistant', 'ai', 'chat', 'conversations', 'navigate'],
      available: () => this.panel.isAvailable(),
      execute: () => this.navigate('assistant'),
    },
    {
      id: 'nav.storage',
      label: $localize`:Command palette action that navigates to file storage:Go to Storage`,
      group: 'navigation',
      icon: 'hard-drive',
      keywords: ['storage', 'files', 'uploads', 'navigate'],
      available: () => this.canReadStorage(),
      execute: () => this.navigate('storage'),
    },
    {
      id: 'nav.audit',
      label: $localize`:Command palette action that navigates to the workspace audit log:Go to Audit Log`,
      group: 'navigation',
      icon: 'shield',
      keywords: ['audit', 'log', 'history', 'security', 'navigate'],
      available: () => this.canReadAudit(),
      execute: () => this.navigate('audit'),
    },
    {
      id: 'nav.profile',
      label: $localize`:Command palette action that navigates to the signed-in user's profile:Go to Profile`,
      group: 'navigation',
      icon: 'user',
      keywords: ['profile', 'account', 'me', 'navigate'],
      available: () => this.authenticated(),
      execute: () => this.navigate('profile'),
    },
    {
      id: 'nav.workspaces',
      label: $localize`:Command palette action that navigates to the workspace picker:Switch Workspace`,
      group: 'navigation',
      icon: 'layout-grid',
      keywords: ['workspaces', 'switch', 'change', 'navigate'],
      available: () => this.authenticated(),
      execute: () => void this.router.navigate(['/workspaces']),
    },
    {
      id: 'nav.settings',
      label: $localize`:Command palette action that navigates to settings:Go to Settings`,
      group: 'settings',
      icon: 'settings',
      keywords: ['settings', 'preferences', 'personal', 'appearance'],
      available: () => this.authenticated(),
      execute: () => this.navigate('settings/personal/general'),
    },
    {
      id: 'settings.theme.dark',
      label: $localize`:Command palette action that switches to the dark theme:Use Dark Theme`,
      group: 'settings',
      icon: 'moon',
      keywords: ['theme', 'dark', 'appearance', 'mode'],
      available: () => this.theme.theme() === 'light',
      execute: () => this.setTheme('dark'),
    },
    {
      id: 'settings.theme.light',
      label: $localize`:Command palette action that switches to the light theme:Use Light Theme`,
      group: 'settings',
      icon: 'sun',
      keywords: ['theme', 'light', 'appearance', 'mode'],
      available: () => this.theme.theme() === 'dark',
      execute: () => this.setTheme('light'),
    },
    {
      id: 'settings.theme.system',
      label: $localize`:Command palette action that drops the theme choice so the app follows the operating system:Use System Theme`,
      group: 'settings',
      icon: 'monitor',
      keywords: ['theme', 'system', 'auto', 'appearance', 'mode'],
      available: () => this.hasChosenTheme(),
      execute: () => this.useSystemTheme(),
    },
    {
      id: 'settings.pageWidth.full',
      label: $localize`:Command palette action that stretches pages across the full window width:Use Full Page Width`,
      group: 'settings',
      icon: 'move-horizontal',
      keywords: ['page width', 'full', 'wide', 'layout', 'appearance'],
      available: () => this.authenticated() && this.pageWidth.centered(),
      execute: () => this.updatePreference(APPEARANCE_PAGE_WIDTH, 'full'),
    },
    {
      id: 'settings.pageWidth.centered',
      label: $localize`:Command palette action that caps pages to a centered readable column:Use Centered Page Width`,
      group: 'settings',
      icon: 'move-horizontal',
      keywords: ['page width', 'centered', 'narrow', 'layout', 'appearance'],
      available: () => this.authenticated() && !this.pageWidth.centered(),
      execute: () => this.updatePreference(APPEARANCE_PAGE_WIDTH, 'centered'),
    },
    {
      id: 'settings.taskLayout.document',
      label: $localize`:Command palette action that switches the task detail view to the document layout:Use Document Task Layout`,
      group: 'settings',
      icon: 'file-text',
      keywords: ['task layout', 'document', 'task detail', 'appearance'],
      available: () => this.canSwitchTaskLayoutTo('document'),
      execute: () => this.taskLayout.switchTo('document'),
    },
    {
      id: 'settings.taskLayout.sections',
      label: $localize`:Command palette action that switches the task detail view to the sections layout:Use Sections Task Layout`,
      group: 'settings',
      icon: 'panel-left',
      keywords: ['task layout', 'sections', 'task detail', 'appearance'],
      available: () => this.canSwitchTaskLayoutTo('sections'),
      execute: () => this.taskLayout.switchTo('sections'),
    },
    {
      id: 'actions.assistant',
      label: $localize`:Command palette action that opens the AI assistant:Open Assistant`,
      group: 'actions',
      icon: 'sparkles',
      keywords: ['assistant', 'ai', 'chat'],
      available: () => this.panel.isAvailable(),
      execute: () => this.panel.open(),
    },
    {
      id: 'actions.createTask',
      label: $localize`:Command palette action that opens the create-task dialog:Create Task`,
      group: 'actions',
      icon: 'circle-plus',
      shortcut: ['c', 't'],
      keywords: ['create', 'task', 'new', 'add'],
      available: () => this.canCreateTasks(),
      execute: () => this.createTask(),
    },
    {
      id: 'actions.createPage',
      label: $localize`:Command palette action that creates a new page:Create Page`,
      group: 'actions',
      icon: 'file-plus',
      keywords: ['create', 'page', 'doc', 'document', 'new', 'add'],
      available: () => this.canCreatePages(),
      execute: () => this.createPage(),
    },
  ];

  constructor() {
    injectCommands(() => {
      return this.commands
        .filter((command) => command.available?.() ?? true)
        .map(toCommand);
    }, 'global');

    this.registerPinShortcuts();
  }

  private hasChosenTheme(): boolean {
    if (!this.authenticated()) return false;

    const source = this.preferences.sourceFor(APPEARANCE_THEME);

    return source !== undefined && source !== 'default';
  }

  private canSwitchTaskLayoutTo(layout: TaskDetailLayout): boolean {
    const isCurrentLayout = this.taskLayout.layout() === layout;

    return this.taskLayout.canSwitch() && !isCurrentLayout;
  }

  private setTheme(theme: Theme) {
    if (!this.authenticated()) {
      this.theme.set(theme);

      return;
    }

    this.updatePreference(APPEARANCE_THEME, theme);
  }

  private useSystemTheme() {
    this.preferences.deleteValue(APPEARANCE_THEME, 'global').subscribe();
  }

  private updatePreference(key: string, value: string) {
    this.preferences.updateValue(key, 'global', value).subscribe();
  }

  private registerPinShortcuts() {
    effect(() => {
      const event = this.keyboard.keyDown();

      if (!event || event.key.toLowerCase() !== 'p') return;
      if (event.ctrlKey || event.metaKey || event.altKey) return;

      if (!this.authenticated()) return;

      const typing = isTypingTarget(event);

      if (typing) return;

      untracked(() => {
        const task = this.currentTask();

        if (!task) return;

        event.preventDefault();

        if (event.shiftKey) {
          this.pinCommands.requestScopeMenu();

          return;
        }

        this.pinCommands.pinForSelf(task.id);
      });
    });
  }

  private createTask() {
    this.dialog.open(CreateTaskDialogComponent, {
      width: CreateTaskDialogComponent.width,
      height: CreateTaskDialogComponent.height,
      panelClass: CreateTaskDialogComponent.panelClass,
    });
  }

  private createPage() {
    this.pageCommands
      .create({ kind: PageKind.document })
      .subscribe((page) => this.navigate(`pages/${page.slug}`));
  }

  private navigate(path: string) {
    const ws = this.workspace.getWorkspaceRoute();
    if (ws) {
      void this.router.navigate(['/', ws, ...path.split('/')]);
    }
  }
}

function toCommand({ available: _available, ...command }: AvailableCommand) {
  return command;
}

function isTypingTarget(event: KeyboardEvent): boolean {
  const target = event.target as HTMLElement | null;

  if (!target) return false;

  const tag = target.tagName;

  return tag === 'INPUT' || tag === 'TEXTAREA' || target.isContentEditable;
}
