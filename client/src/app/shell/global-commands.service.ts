import { Injectable, OnDestroy, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommandRegistry } from '@core/services/command-registry.service';
import { WorkspaceService } from '@core/services/workspace.service';

@Injectable()
export class GlobalCommandsService implements OnDestroy {
  private router = inject(Router);
  private registry = inject(CommandRegistry);
  private workspace = inject(WorkspaceService);

  private readonly commandIds = [
    'nav.projects',
    'nav.tasks',
    'nav.boards',
    'nav.sprints',
    'nav.users',
    'nav.settings',
  ];

  constructor() {
    this.registry.register([
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
