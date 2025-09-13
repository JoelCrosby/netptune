import { NgModule } from '@angular/core';
import { LetDirective, PushPipe } from '@ngrx/component';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { SharedModule } from '@shared/shared.module';
import { StaticModule } from '@static/static.module';
import { BoardsRoutingModule } from './boards-routing.module';
import { BoardGroupCardComponent } from './components/board-group-card/board-group-card.component';
import { BoardGroupTagsComponent } from './components/board-group-tags/board-group-tags.component';
import { BoardGroupTaskInlineComponent } from './components/board-group-task-inline/board-group-task-inline.component';
import { BoardGroupUsersComponent } from './components/board-group-users/board-group-users.component';
import { BoardGroupComponent } from './components/board-group/board-group.component';
import { BoardGroupsFlaggedComponent } from './components/board-groups-flagged/board-groups-flagged.component';
import { BoardGroupsSearchComponent } from './components/board-groups-search/board-groups-search.component';
import { BoardGroupsSelectionComponent } from './components/board-groups-selection/board-groups-selection.component';
import { BoardsGridComponent } from './components/boards-grid/boards-grid.component';
import { CreateBoardGroupComponent } from './components/create-board-group/create-board-group.component';
import { CreateBoardComponent } from './components/create-board/create-board.component';
import { MoveTasksDialogComponent } from './components/move-tasks-dialog/move-tasks-dialog.component';
import { ReassignTasksDialogComponent } from './components/reassign-tasks-dialog/reassign-tasks-dialog.component';
import { BoardsEffects } from './store/boards/boards.effects';
import { boardsReducer } from './store/boards/boards.reducer';
import { BoardGroupsEffects } from './store/groups/board-groups.effects';
import { boardGroupsReducer } from './store/groups/board-groups.reducer';
import { BoardGroupsViewComponent } from './views/board-groups-view/board-groups-view.component';
import { BoardsViewComponent } from './views/boards-view/boards-view.component';

@NgModule({
    imports: [
        SharedModule,
        StaticModule,
        LetDirective,
        PushPipe,
        StoreModule.forFeature('boards', boardsReducer),
        StoreModule.forFeature('boardgroups', boardGroupsReducer),
        EffectsModule.forFeature([BoardsEffects, BoardGroupsEffects]),
        BoardsRoutingModule,
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
    providers: [],
})
export class BoardsModule {}
