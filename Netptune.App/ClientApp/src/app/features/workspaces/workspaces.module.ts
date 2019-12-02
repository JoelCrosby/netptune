import { NgModule } from '@angular/core';
import { WorkspacesRoutingModule } from './workspaces-routing.module';
import { SharedModule } from '@app/shared/shared.module';

import { WorkspacesComponent } from './index/workspaces.index.component';
import { StoreModule } from '@ngrx/store';
import { EffectsModule } from '@ngrx/effects';
import { workspacesReducer } from './store/workspaces.reducer';
import { WorkspacesEffects } from './store/workspaces.effects';
import { WorkspacesService } from './store/workspaces.service';
import { StaticModule } from '@app/static/static.module';
import { ShellComponent } from './shell/shell.component';

@NgModule({
  declarations: [WorkspacesComponent, ShellComponent],
  imports: [
    SharedModule,
    StaticModule,
    StoreModule.forFeature('workspaces', workspacesReducer),
    EffectsModule.forFeature([WorkspacesEffects]),
    WorkspacesRoutingModule,
  ],
  providers: [WorkspacesService],
})
export class WorkspacesModule {}
