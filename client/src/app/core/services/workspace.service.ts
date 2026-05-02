import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class WorkspaceService {
  private readonly nonWorkspaceRoutes = new Set(['auth', 'workspaces']);

  currentWorkspace = signal<string | null>(null);

  setWorkspace(workspace: string | null) {
    this.currentWorkspace.set(workspace);
  }

  getWorkspaceRoute(): string | null {
    const url = window.location.pathname;
    const parts = url.split('/').filter((p) => !!p);

    if (parts.length >= 1) {
      const workspace = parts[0];

      if (!this.nonWorkspaceRoutes.has(workspace)) {
        return workspace;
      }
    }

    if (this.currentWorkspace()) {
      return this.currentWorkspace();
    }

    return null;
  }
}
