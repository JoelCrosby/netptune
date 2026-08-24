import { Routes } from '@angular/router';
import { pinsReadGuard } from './guards/pins-read.guard';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [pinsReadGuard],
    loadComponent: () => import('./views/pinned-view/pinned-view.component').then((m) => m.PinnedViewComponent),
  },
];
