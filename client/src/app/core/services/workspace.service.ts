import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class WorkspaceService {
  currentWorkspace = signal<string | null>(null);

  setWorkspace(workspace: string | null) {
    this.currentWorkspace.set(workspace);
  }

  getWorkspaceRoute(): string | null {
    const url = window.location.pathname;
    const parts = url.split('/').filter((p) => !!p);

    if (parts.length >= 1) {
      const workspace = parts[0];

      if (workspace !== 'workspaces') {
        return workspace;
      }
    }

    if (this.currentWorkspace()) {
      return this.currentWorkspace();
    }

    return null;
  }
}
