import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ProjectTasksRoutingModule } from './project-tasks-routing.module';

import { ProjectTasksComponent } from './index/project-tasks.index.component';
import { TaskListComponent } from './components/task-list/task-list.component';

import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';

import { DragDropModule } from '@angular/cdk/drag-drop';

@NgModule({
  declarations: [
    ProjectTasksComponent,
    TaskListComponent
  ],
  imports: [
    CommonModule,
    ProjectTasksRoutingModule,
    MatProgressSpinnerModule,
    MatExpansionModule,
    MatChipsModule,
    DragDropModule
  ]
})
export class ProjectTasksModule { }
