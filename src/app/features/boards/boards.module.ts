import { BoardGroupsService } from './store/groups/board-groups.service';
import { BoardGroupsEffects } from './store/groups/board-groups.effects';
import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { StaticModule } from '@static/static.module';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { BoardsRoutingModule } from './boards-routing.module';
import { BoardsEffects } from './store/boards/boards.effects';
import { boardsReducer } from './store/boards/boards.reducer';
import { BoardsService } from './store/boards/boards.service';
import { BoardGroupsViewComponent } from './views/board-groups-view/board-groups-view.component';
import { boardGroupsReducer } from './store/groups/board-groups.reducer';
import { BoardGroupComponent } from './components/board-group/board-group.component';
import { BoardGroupTaskInlineComponent } from './components/board-group-task-inline/board-group-task-inline.component';
import { BoardGroupCardComponent } from './components/board-group-card/board-group-card.component';
import { CreateBoardGroupComponent } from './components/create-board-group/create-board-group.component';
import { BoardsViewComponent } from './views/boards-view/boards-view.component';
import { BoardsGridComponent } from './components/boards-grid/boards-grid.component';
import { BoardGroupUsersComponent } from './components/board-group-users/board-group-users.component';
import { CreateBoardComponent } from './components/create-board/create-board.component';
import { BoardGroupHubService } from './store/groups/board-groups.hub.service';

@NgModule({
  declarations: [
    BoardGroupsViewComponent,
    BoardGroupComponent,
    BoardGroupTaskInlineComponent,
    BoardGroupCardComponent,
    CreateBoardGroupComponent,
    BoardsViewComponent,
    BoardsGridComponent,
    BoardGroupUsersComponent,
    CreateBoardComponent,
  ],
  imports: [
    SharedModule,
    StaticModule,
    StoreModule.forFeature('boards', boardsReducer),
    StoreModule.forFeature('boardgroups', boardGroupsReducer),
    EffectsModule.forFeature([BoardsEffects, BoardGroupsEffects]),
    BoardsRoutingModule,
  ],
  providers: [BoardsService, BoardGroupsService, BoardGroupHubService],
})
export class BoardsModule {}
