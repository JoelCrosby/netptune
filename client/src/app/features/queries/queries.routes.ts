import { Routes } from '@angular/router';
import { queriesReadGuard } from './guards/queries-read.guard';
import { queriesWriteGuard } from './guards/queries-write.guard';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [queriesReadGuard],
    loadComponent: () => import('./views/queries-view/queries-view.component').then((m) => m.QueriesViewComponent),
  },
  {
    path: 'new',
    canActivate: [queriesReadGuard, queriesWriteGuard],
    loadComponent: () => import('./views/query-form-view/query-form-view.component').then((m) => m.QueryFormViewComponent),
    data: {
      back: $localize`:Link back to the query list from a single query:Back to Queries`,
    },
  },
  {
    path: ':slug/edit',
    canActivate: [queriesReadGuard, queriesWriteGuard],
    loadComponent: () => import('./views/query-form-view/query-form-view.component').then((m) => m.QueryFormViewComponent),
    data: {
      back: $localize`:Link back to the query list from a single query:Back to Queries`,
    },
  },
  {
    path: ':slug',
    canActivate: [queriesReadGuard],
    loadComponent: () => import('./views/query-detail-view/query-detail-view.component').then((m) => m.QueryDetailViewComponent),
    data: {
      back: $localize`:Link back to the query list from a single query:Back to Queries`,
    },
  },
];
