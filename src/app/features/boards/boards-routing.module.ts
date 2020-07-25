import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { BoardGroupsViewComponent } from './views/board-groups-view/board-groups-view.component';
import { BoardsViewComponent } from './views/boards-view/boards-view.component';

const routes: Routes = [
  { path: '', component: BoardsViewComponent, pathMatch: 'full' },
  {
    path: ':id',
    component: BoardGroupsViewComponent,
  },
  { path: '**', component: BoardsViewComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class BoardsRoutingModule {}
