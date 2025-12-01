import { NgModule } from '@angular/core';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { BoardsRoutingModule } from './boards-routing.module';
import { BoardsEffects } from './store/boards/boards.effects';
import { boardsReducer } from './store/boards/boards.reducer';
import { BoardGroupsEffects } from './store/groups/board-groups.effects';
import { boardGroupsReducer } from './store/groups/board-groups.reducer';

@NgModule({
  imports: [
    StoreModule.forFeature('boards', boardsReducer),
    StoreModule.forFeature('boardgroups', boardGroupsReducer),
    EffectsModule.forFeature([BoardsEffects, BoardGroupsEffects]),
    BoardsRoutingModule,
  ],
})
export class BoardsModule {}
