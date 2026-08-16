import { Routes } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';
import { workspaceSettingsGuard } from '@settings/guards/workspace-settings.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [workspaceSettingsGuard],
    data: { permission: PERMISSIONS.storage.read },
    loadComponent: () =>
      import('./views/storage-view.component').then(
        (m) => m.StorageViewComponent
      ),
  },
];
