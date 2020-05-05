import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { BoardsViewComponent } from './views/boards-view/boards-view.component';

const routes: Routes = [{ path: '**', component: BoardsViewComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class BoardsRoutingModule {}
