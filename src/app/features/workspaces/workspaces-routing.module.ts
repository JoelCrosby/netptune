import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { WorkspacesViewComponent } from '@workspaces/views/workspaces-view/workspaces-view.component';

const routes: Routes = [{ path: '**', component: WorkspacesViewComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class WorkspacesRoutingModule {}
