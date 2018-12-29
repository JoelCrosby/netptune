import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ProjectTasksComponent } from './components/project-tasks/project-tasks.component';

const routes: Routes = [
  { path: '**', component: ProjectTasksComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ProjectTasksRoutingModule { }
