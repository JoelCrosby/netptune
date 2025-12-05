import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProjectDetailViewComponent } from './views/project-detail-view/project-detail-view.component';
import { ProjectsViewComponent } from './views/projects-view/projects-view.component';
import { projectDetailGuard } from './guards/project-detail.guard';

const routes: Routes = [
  { path: '', component: ProjectsViewComponent },
  {
    path: ':id',
    component: ProjectDetailViewComponent,
    canActivate: [projectDetailGuard],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class ProjectsRoutingModule {}
