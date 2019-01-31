import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkspacesRoutingModule } from './workspaces-routing.module';
import { SharedModule } from '../shared/shared.module';

import { WorkspacesComponent } from './index/workspaces.index.component';
import { WorkspaceDialogComponent } from '../../dialogs/workspace-dialog/workspace-dialog.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    WorkspacesComponent,
    WorkspaceDialogComponent
  ],
  entryComponents: [
    WorkspaceDialogComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    SharedModule,
    WorkspacesRoutingModule
  ]
})
export class WorkspacesModule { }
