import { NgModule } from '@angular/core';
import { WorkspacesRoutingModule } from './workspaces-routing.module';
import { SharedModule } from '@shared/shared.module';

import { WorkspaceListComponent } from './components/workspace-list/workspace-list.component';
import { StaticModule } from '@static/static.module';
import { ShellComponent } from './components/shell/shell.component';
import { WorkspacesViewComponent } from './views/workspaces-view/workspaces-view.component';
import { WorkspaceListItemComponent } from './components/workspace-list-item/workspace-list-item.component';
import { CreateWorkspaceListItemComponent } from './components/create-workspace-list-item/create-workspace-list-item.component';

@NgModule({
  declarations: [
    WorkspaceListComponent,
    ShellComponent,
    WorkspacesViewComponent,
    WorkspaceListItemComponent,
    CreateWorkspaceListItemComponent,
  ],
  imports: [SharedModule, StaticModule, WorkspacesRoutingModule],
})
export class WorkspacesModule {}
