import { ProjectDetailViewComponent } from './views/project-detail-view/project-detail-view.component';
import { ProjectsViewComponent } from './views/projects-view/projects-view.component';
import { projectDetailGuard } from './guards/project-detail.guard';
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', component: ProjectsViewComponent },
  {
    path: ':id',
    component: ProjectDetailViewComponent,
    canActivate: [projectDetailGuard],
    data: { back: 'Back to Projects' },
  },
];
