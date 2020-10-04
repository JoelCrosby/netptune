import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ProjectTasksViewComponent } from './views/project-tasks-view/project-tasks-view.component';

const routes: Routes = [{ path: '**', component: ProjectTasksViewComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class ProjectTasksRoutingModule {}
