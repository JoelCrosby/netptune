import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { StaticModule } from '@app/static/static.module';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { BoardsRoutingModule } from './boards-routing.module';
import { BoardsEffects } from './store/boards.effects';
import { boardsReducer } from './store/boards.reducer';
import { BoardsService } from './store/boards.service';
import { BoardsViewComponent } from './views/boards-view/boards-view.component';

@NgModule({
  declarations: [BoardsViewComponent],
  imports: [
    SharedModule,
    StaticModule,
    StoreModule.forFeature('boards', boardsReducer),
    EffectsModule.forFeature([BoardsEffects]),
    BoardsRoutingModule,
  ],
  providers: [BoardsService],
})
export class BoardsModule {}
