import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkspacesRoutingModule } from './workspaces-routing.module';

import { WorkspacesComponent } from './index/workspaces.index.component';
import {
  MatCardModule,
  MatIconModule,
  MatButtonModule,
  MatFormFieldModule,
  MatDialogModule,
  MatSnackBarModule,
  MatInputModule
} from '@angular/material';
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
    MatCardModule,
    MatIconModule,
    MatInputModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSnackBarModule,
    MatDialogModule,
    WorkspacesRoutingModule
  ]
})
export class WorkspacesModule { }
