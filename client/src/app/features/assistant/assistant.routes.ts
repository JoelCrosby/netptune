import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./views/assistant-view/assistant-view.component').then(
        (m) => m.AssistantViewComponent
      ),
    pathMatch: 'full',
    data: {
      title: $localize`:Page title for the assistant chat page:Assistant`,
    },
  },
];
