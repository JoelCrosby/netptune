import { Routes } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { workspaceSettingsGuard } from '@settings/guards/workspace-settings.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [workspaceSettingsGuard],
    data: { permission: netptunePermissions.storage.read },
    loadComponent: () => import('./views/storage-view.component').then((m) => m.StorageViewComponent),
  },
];
