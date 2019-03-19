import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProjectTasksRoutingModule } from './project-tasks-routing.module';
import { SharedModule } from '@app/shared/shared.module';

import { ProjectTasksComponent } from './index/project-tasks.index.component';
import { TaskListItemComponent } from './components/task-list-item/task-list-item.component';

import { DragDropModule } from '@angular/cdk/drag-drop';

@NgModule({
  declarations: [ProjectTasksComponent, TaskListItemComponent],
  imports: [CommonModule, ProjectTasksRoutingModule, SharedModule, DragDropModule],
})
export class ProjectTasksModule {}
