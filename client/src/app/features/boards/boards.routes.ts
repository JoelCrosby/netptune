import { Routes } from '@angular/router';
import { BoardGroupsViewComponent } from './views/board-groups-view/board-groups-view.component';
import { BoardsViewComponent } from './views/boards-view/boards-view.component';

export const routes: Routes = [
  { path: '', component: BoardsViewComponent, pathMatch: 'full' },
  {
    path: ':id',
    component: BoardGroupsViewComponent,
    runGuardsAndResolvers: 'always',
    data: {
      back: 'Back to Boards',
    },
  },
  { path: '**', component: BoardsViewComponent },
];
