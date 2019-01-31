import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProjectTasksRoutingModule } from './project-tasks-routing.module';
import { SharedModule } from '../shared/shared.module';

import { ProjectTasksComponent } from './index/project-tasks.index.component';
import { TaskListComponent } from './components/task-list/task-list.component';


import { DragDropModule } from '@angular/cdk/drag-drop';

@NgModule({
  declarations: [
    ProjectTasksComponent,
    TaskListComponent
  ],
  imports: [
    CommonModule,
    ProjectTasksRoutingModule,
    SharedModule,
    DragDropModule
  ]
})
export class ProjectTasksModule { }
