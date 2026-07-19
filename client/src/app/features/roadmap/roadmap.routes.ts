import { Routes } from '@angular/router';
import { roadmapReadGuard } from './guards/roadmap-read.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [roadmapReadGuard],
    loadComponent: () =>
      import('./views/roadmap-view.component').then(
        (module) => module.RoadmapViewComponent
      ),
  },
];
