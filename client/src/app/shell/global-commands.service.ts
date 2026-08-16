import {
  Injectable,
  OnDestroy,
  effect,
  inject,
  untracked,
} from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { hasPermission } from '@core/auth/has-permission';
import { Router } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';
import { AiPanelService } from '@core/services/ai-panel.service';
import { CommandRegistry } from '@core/services/command-registry.service';
import { DialogService } from '@core/services/dialog.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';

@Injectable()
export class GlobalCommandsService implements OnDestroy {
  private router = inject(Router);
  private registry = inject(CommandRegistry);
  private workspace = inject(WorkspaceService);
  private panel = inject(AiPanelService);
  private dialog = inject(DialogService);
  private canCreateTasks = hasPermission(PERMISSIONS.tasks.create);
  private createTaskCommandRegistered = false;
  private canReadAutomations = hasPermission(PERMISSIONS.automations.read);
  private automationCommandRegistered = false;
  private canReadStorage = hasPermission(PERMISSIONS.storage.read);
  private storageCommandRegistered = false;
  private assistantCommandRegistered = false;
  private authenticated = inject(SessionService).isAuthenticated;
  private userCommandsRegistered = false;

  private readonly commandIds = [
    'nav.dashboard',
    'nav.projects',
    'nav.tasks',
    'nav.boards',
    'nav.sprints',
    'nav.automations',
    'nav.users',
    'nav.settings',
    'nav.storage',
    'actions.assistant',
    'actions.createTask',
  ];

  constructor() {
    this.registry.register([
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
        execute: () => this.navigate('sprints'),
      },
    ]);

    effect(() => {
      const authenticated = this.authenticated();

      if (authenticated && !this.userCommandsRegistered) {
        untracked(() =>
          this.registry.register([
            {
              id: 'nav.dashboard',
              label: $localize`:Command palette action that navigates to the dashboard:Go to Dashboard`,
              group: 'navigation',
              icon: 'layout-dashboard',
              shortcut: ['g', 'd'],
              keywords: ['dashboard', 'home', 'assigned to me', 'navigate'],
              execute: () => this.navigate('dashboard'),
            },
            {
              id: 'nav.users',
              label: $localize`:Command palette action that navigates to the member list:Go to Users`,
              group: 'navigation',
              icon: 'users',
              keywords: ['users', 'members', 'navigate'],
              execute: () => this.navigate('users'),
            },
            {
              id: 'nav.settings',
              label: $localize`:Command palette action that navigates to settings:Go to Settings`,
              group: 'settings',
              icon: 'settings',
              keywords: ['settings', 'preferences'],
              execute: () => this.navigate('settings'),
            },
          ])
        );

        this.userCommandsRegistered = true;
      }

      if (!authenticated && this.userCommandsRegistered) {
        untracked(() =>
          this.registry.unregister([
            'nav.dashboard',
            'nav.users',
            'nav.settings',
          ])
        );

        this.userCommandsRegistered = false;
      }
    });

    effect(() => {
      const isAvailable = this.panel.isAvailable();

      if (isAvailable && !this.assistantCommandRegistered) {
        untracked(() =>
          this.registry.register([
            {
              id: 'actions.assistant',
              label: $localize`:Command palette action that opens the AI assistant:Open Assistant`,
              group: 'actions',
              icon: 'sparkles',
              keywords: ['assistant', 'ai', 'chat'],
              execute: () => this.panel.open(),
            },
          ])
        );

        this.assistantCommandRegistered = true;
      }

      if (!isAvailable && this.assistantCommandRegistered) {
        untracked(() => this.registry.unregister(['actions.assistant']));

        this.assistantCommandRegistered = false;
      }
    });

    effect(() => {
      const canCreate = this.canCreateTasks();

      if (canCreate && !this.createTaskCommandRegistered) {
        untracked(() =>
          this.registry.register([
            {
              id: 'actions.createTask',
              label: $localize`:Command palette action that opens the create-task dialog:Create Task`,
              group: 'actions',
              icon: 'circle-plus',
              shortcut: ['c', 't'],
              keywords: ['create', 'task', 'new', 'add'],
              execute: () => this.createTask(),
            },
          ])
        );

        this.createTaskCommandRegistered = true;
      }

      if (!canCreate && this.createTaskCommandRegistered) {
        untracked(() => this.registry.unregister(['actions.createTask']));

        this.createTaskCommandRegistered = false;
      }
    });

    effect(() => {
      const canRead = this.canReadAutomations();

      if (canRead && !this.automationCommandRegistered) {
        untracked(() =>
          this.registry.register([
            {
              id: 'nav.automations',
              label: $localize`:Command palette action that navigates to the automation list:Go to Automations`,
              group: 'navigation',
              icon: 'workflow',
              shortcut: ['g', 'a'],
              keywords: ['automations', 'automation', 'rules', 'workflow'],
              execute: () => this.navigate('automations'),
            },
          ])
        );
        this.automationCommandRegistered = true;
      }

      if (!canRead && this.automationCommandRegistered) {
        untracked(() => this.registry.unregister(['nav.automations']));
        this.automationCommandRegistered = false;
      }
    });

    effect(() => {
      const canRead = this.canReadStorage();

      if (canRead && !this.storageCommandRegistered) {
        untracked(() =>
          this.registry.register([
            {
              id: 'nav.storage',
              label: $localize`:Command palette action that navigates to file storage:Go to Storage`,
              group: 'navigation',
              icon: 'hard-drive',
              keywords: ['storage', 'files', 'uploads', 'navigate'],
              execute: () => this.navigate('storage'),
            },
          ])
        );

        this.storageCommandRegistered = true;
      }

      if (!canRead && this.storageCommandRegistered) {
        untracked(() => this.registry.unregister(['nav.storage']));

        this.storageCommandRegistered = false;
      }
    });
  }

  ngOnDestroy() {
    this.registry.unregister(this.commandIds);
  }

  private createTask() {
    this.dialog.open(CreateTaskDialogComponent, {
      width: CreateTaskDialogComponent.width,
    });
  }

  private navigate(path: string) {
    const ws = this.workspace.getWorkspaceRoute();
    if (ws) {
      void this.router.navigate(['/', ws, path]);
    }
  }
}
