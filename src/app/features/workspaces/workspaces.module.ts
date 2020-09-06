import { NgModule } from '@angular/core';
import { WorkspacesRoutingModule } from './workspaces-routing.module';
import { SharedModule } from '@shared/shared.module';

import { WorkspaceListComponent } from './components/workspace-list/workspace-list.component';
import { StaticModule } from '@static/static.module';
import { ShellComponent } from './components/shell/shell.component';
import { WorkspacesViewComponent } from './views/workspaces-view/workspaces-view.component';

@NgModule({
  declarations: [
    WorkspaceListComponent,
    ShellComponent,
    WorkspacesViewComponent,
  ],
  imports: [SharedModule, StaticModule, WorkspacesRoutingModule],
})
export class WorkspacesModule {}
