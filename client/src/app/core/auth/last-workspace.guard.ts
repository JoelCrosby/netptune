import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { WorkspaceService } from '@core/services/workspace.service';
import { WorkspacesService } from '@core/store/workspaces/workspaces.service';
import { Store } from '@ngrx/store';
import { firstValueFrom } from 'rxjs';
import { selectIsAuthenticated } from '../store/auth/auth.selectors';

export const lastWorkspaceGuard: CanActivateFn = async () => {
  const store = inject(Store);
  const router = inject(Router);
  const workspaces = inject(WorkspacesService);
  const workspaceService = inject(WorkspaceService);

  const isAuthenticated = store.selectSignal(selectIsAuthenticated)();
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
