import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { BoardGroupsViewComponent } from './views/board-groups-view/board-groups-view.component';

const routes: Routes = [{ path: '**', component: BoardGroupsViewComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class BoardsRoutingModule {}
