import { NgModule } from '@angular/core';
import { WorkspacesRoutingModule } from './workspaces-routing.module';
import { SharedModule } from '@app/shared/shared.module';

import { WorkspacesComponent } from './index/workspaces.index.component';
import { StaticModule } from '@app/static/static.module';
import { ShellComponent } from './shell/shell.component';

@NgModule({
  declarations: [WorkspacesComponent, ShellComponent],
  imports: [SharedModule, StaticModule, WorkspacesRoutingModule],
})
export class WorkspacesModule {}
