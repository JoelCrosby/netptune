import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ProjectTasksComponent } from './index/project-tasks.index.component';

const routes: Routes = [
  { path: '**', component: ProjectTasksComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ProjectTasksRoutingModule { }
