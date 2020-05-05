import { BoardGroupsService } from './store/groups/board-groups.service';
import { BoardGroupsEffects } from './store/groups/board-groups.effects';
import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { StaticModule } from '@app/static/static.module';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { BoardsRoutingModule } from './boards-routing.module';
import { BoardsEffects } from './store/boards/boards.effects';
import { boardsReducer } from './store/boards/boards.reducer';
import { BoardsService } from './store/boards/boards.service';
import { BoardsViewComponent } from './views/boards-view/boards-view.component';
import { boardGroupsReducer } from './store/groups/board-groups.reducer';
import { BoardGroupComponent } from './components/board-group/board-group.component';
import { BoardGroupTaskInlineComponent } from './components/board-group-task-inline/board-group-task-inline.component';
import { BoardGroupCardComponent } from './components/board-group-card/board-group-card.component';

@NgModule({
  declarations: [
    BoardsViewComponent,
    BoardGroupComponent,
    BoardGroupTaskInlineComponent,
    BoardGroupCardComponent,
  ],
  imports: [
    SharedModule,
    StaticModule,
    StoreModule.forFeature('boards', boardsReducer),
    StoreModule.forFeature('boardgroups', boardGroupsReducer),
    EffectsModule.forFeature([BoardsEffects, BoardGroupsEffects]),
    BoardsRoutingModule,
  ],
  providers: [BoardsService, BoardGroupsService],
})
export class BoardsModule {}
