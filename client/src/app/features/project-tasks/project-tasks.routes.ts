import { Routes } from '@angular/router';
import { ProjectTasksViewComponent } from './views/project-tasks-view/project-tasks-view.component';

export const routes: Routes = [
  { path: '**', component: ProjectTasksViewComponent },
];
