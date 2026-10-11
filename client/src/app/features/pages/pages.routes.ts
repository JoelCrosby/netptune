import { Routes } from '@angular/router';
import { pagesReadGuard } from './guards/pages-read.guard';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    canActivate: [pagesReadGuard],
    loadComponent: () => import('./views/pages-layout-view/pages-layout-view.component').then((m) => m.PagesLayoutViewComponent),
    children: [
      { path: '', pathMatch: 'full', loadComponent: () => import('./views/pages-home-view/pages-home-view.component').then((m) => m.PagesHomeViewComponent) },
      { path: 'archive', loadComponent: () => import('./views/pages-archive-view/pages-archive-view.component').then((m) => m.PagesArchiveViewComponent) },
      { path: ':slug', loadComponent: () => import('./views/page-detail-view/page-detail-view.component').then((m) => m.PageDetailViewComponent) },
    ],
  },
];
