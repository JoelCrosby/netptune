import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BoardsViewComponent } from './views/boards-view/boards-view.component';
import { BoardsRoutingModule } from './boards-routing.module';

@NgModule({
  declarations: [BoardsViewComponent],
  imports: [CommonModule, BoardsRoutingModule],
  providers: [],
})
export class BoardsModule {}
