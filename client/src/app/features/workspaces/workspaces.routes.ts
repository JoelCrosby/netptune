import { Routes } from '@angular/router';
import { WorkspacesViewComponent } from '@workspaces/views/workspaces-view/workspaces-view.component';

export const routes: Routes = [
  { path: '**', component: WorkspacesViewComponent },
];
