import { Routes } from '@angular/router';
import { calendarReadGuard } from './guards/calendar-read.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [calendarReadGuard],
    loadComponent: () =>
      import('./views/calendar-view/calendar-view.component').then(
        (module) => module.CalendarViewComponent
      ),
  },
];
