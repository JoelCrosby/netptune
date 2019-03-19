import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { WorkspacesComponent } from './index/workspaces.index.component';

const routes: Routes = [
  { path: '**', component: WorkspacesComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class WorkspacesRoutingModule { }
