import {
  Injectable,
  OnDestroy,
  effect,
  inject,
  untracked,
} from '@angular/core';
import { Router } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { CommandRegistry } from '@core/services/command-registry.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';

@Injectable()
export class GlobalCommandsService implements OnDestroy {
  private router = inject(Router);
  private registry = inject(CommandRegistry);
  private workspace = inject(WorkspaceService);
  private store = inject(Store);
  private canReadAutomations = this.store.selectSignal(
    selectHasPermission(netptunePermissions.automations.read)
  );
  private automationCommandRegistered = false;
  private canReadStorage = this.store.selectSignal(
    selectHasPermission(netptunePermissions.storage.read)
  );
  private storageCommandRegistered = false;

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
  ];

  constructor() {
    this.registry.register([
      {
        id: 'nav.dashboard',
        label: 'Go to Dashboard',
        group: 'navigation',
        icon: 'layout-dashboard',
        shortcut: 'G D',
        keywords: ['dashboard', 'home', 'assigned to me', 'navigate'],
        execute: () => this.navigate('dashboard'),
      },
      {
        id: 'nav.projects',
        label: 'Go to Projects',
        group: 'navigation',
        icon: 'folder-open',
        shortcut: 'G P',
        keywords: ['projects', 'navigate'],
        execute: () => this.navigate('projects'),
      },
      {
        id: 'nav.tasks',
        label: 'Go to Tasks',
        group: 'navigation',
        icon: 'hash',
        shortcut: 'G T',
        keywords: ['tasks', 'navigate'],
        execute: () => this.navigate('tasks'),
      },
      {
        id: 'nav.boards',
        label: 'Go to Boards',
        group: 'navigation',
        icon: 'kanban',
        shortcut: 'G B',
        keywords: ['boards', 'navigate'],
        execute: () => this.navigate('boards'),
      },
      {
        id: 'nav.sprints',
        label: 'Go to Sprints',
        group: 'navigation',
        icon: 'layers',
        shortcut: 'G S',
        keywords: ['sprints', 'navigate'],
        execute: () => this.navigate('sprints'),
      },
      {
        id: 'nav.users',
        label: 'Go to Users',
        group: 'navigation',
        icon: 'users',
        keywords: ['users', 'members', 'navigate'],
        execute: () => this.navigate('users'),
      },
      {
        id: 'nav.settings',
        label: 'Go to Settings',
        group: 'settings',
        icon: 'settings',
        keywords: ['settings', 'preferences'],
        execute: () => this.navigate('settings'),
      },
    ]);

    effect(() => {
      const canRead = this.canReadAutomations();

      if (canRead && !this.automationCommandRegistered) {
        untracked(() =>
          this.registry.register([
            {
              id: 'nav.automations',
              label: 'Go to Automations',
              group: 'navigation',
              icon: 'workflow',
              shortcut: 'G A',
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
              label: 'Go to Storage',
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

  private navigate(path: string) {
    const ws = this.workspace.getWorkspaceRoute();
    if (ws) {
      void this.router.navigate(['/', ws, path]);
    }
  }
}
