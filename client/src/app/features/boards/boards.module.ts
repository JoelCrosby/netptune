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
import { BoardGroupTagsComponent } from './components/board-group-tags/board-group-tags.component';
import { BoardGroupsFlaggedComponent } from './components/board-groups-flagged/board-groups-flagged.component';
import { LetModule, PushModule } from '@ngrx/component';
import { BoardGroupsSelectionComponent } from './components/board-groups-selection/board-groups-selection.component';
import { MoveTasksDialogComponent } from './components/move-tasks-dialog/move-tasks-dialog.component';
import { ReassignTasksDialogComponent } from './components/reassign-tasks-dialog/reassign-tasks-dialog.component';
import { BoardGroupsSearchComponent } from './components/board-groups-search/board-groups-search.component';

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
    BoardGroupTagsComponent,
    BoardGroupsFlaggedComponent,
    BoardGroupsSelectionComponent,
    MoveTasksDialogComponent,
    ReassignTasksDialogComponent,
    BoardGroupsSearchComponent,
  ],
  imports: [
    SharedModule,
    StaticModule,
    LetModule,
    PushModule,
    StoreModule.forFeature('boards', boardsReducer),
    StoreModule.forFeature('boardgroups', boardGroupsReducer),
    EffectsModule.forFeature([BoardsEffects, BoardGroupsEffects]),
    BoardsRoutingModule,
  ],
  providers: [BoardsService, BoardGroupsService],
})
export class BoardsModule {}
