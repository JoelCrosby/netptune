import { inject } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { CanActivateFn, Router } from '@angular/router';
import { WorkspaceService } from '@core/services/workspace.service';
import { WorkspacesService } from '@core/services/workspaces-api.service';
import { firstValueFrom } from 'rxjs';

export const lastWorkspaceGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const workspaces = inject(WorkspacesService);
  const workspaceService = inject(WorkspaceService);

  const isAuthenticated = inject(SessionService).isAuthenticated();
  const picker = router.createUrlTree(['/workspaces']);

  if (!isAuthenticated) return picker;

  try {
    const workspaceList = await firstValueFrom(workspaces.get());
    const lastVisited = workspaceList.find(
      (workspace) => workspace.isLastVisited
    );

    if (!lastVisited) return picker;

    workspaceService.setWorkspace(lastVisited.slug);

    return router.createUrlTree(['/', lastVisited.slug]);
  } catch {
    return picker;
  }
};
