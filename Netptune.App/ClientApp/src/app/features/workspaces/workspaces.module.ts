import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkspacesRoutingModule } from './workspaces-routing.module';
import { SharedModule } from '@app/shared/shared.module';

import { WorkspacesComponent } from './index/workspaces.index.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@NgModule({
  declarations: [WorkspacesComponent],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SharedModule, WorkspacesRoutingModule],
})
export class WorkspacesModule {}
