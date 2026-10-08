import { Location } from '@angular/common';
import { inject, Service, signal } from '@angular/core';
import { Router } from '@angular/router';

@Service()
export class WorkspaceService {
  private readonly location = inject(Location);
  private readonly router = inject(Router);
  private readonly nonWorkspaceRoutes = new Set(['auth', 'workspaces']);
  private readonly renamedSlugs = new Map<string, string>();

  currentWorkspace = signal<string | null>(null);

  setWorkspace(workspace: string | null) {
    this.currentWorkspace.set(workspace);
  }

  registerRename(previousSlug: string, slug: string) {
    this.renamedSlugs.delete(slug);
    this.renamedSlugs.set(previousSlug, slug);
  }

  getWorkspaceRoute(): string | null {
    const workspace = this.readWorkspaceRoute();

    if (workspace === null) return null;

    return this.renamedSlugs.get(workspace) ?? workspace;
  }

  private readWorkspaceRoute(): string | null {
    const url = this.destinationPath().split('?')[0];
    const parts = url.split('/').filter((p) => !!p);

    if (parts.length >= 1) {
      const workspace = parts[0];

      if (!this.nonWorkspaceRoutes.has(workspace)) {
        return workspace;
      }
    }

    return this.currentWorkspace();
  }

  private destinationPath(): string {
    const navigation = this.router.currentNavigation();

    if (navigation === null) {
      return this.location.path();
    }

    const destination = navigation.finalUrl ?? navigation.extractedUrl;

    return this.router.serializeUrl(destination);
  }
}
