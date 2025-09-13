import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { StaticModule } from '@static/static.module';
import { TaskInlineComponent } from './components/task-inline/task-inline.component';
import { TaskListGroupComponent } from './components/task-list-group/task-list-group.component';
import { TaskListItemComponent } from './components/task-list-item/task-list-item.component';
import { TaskListComponent } from './components/task-list/task-list.component';
import { ProjectTasksRoutingModule } from './project-tasks-routing.module';
import { ProjectTasksViewComponent } from './views/project-tasks-view/project-tasks-view.component';

@NgModule({
    imports: [SharedModule, StaticModule, ProjectTasksRoutingModule, ProjectTasksViewComponent,
        TaskListGroupComponent,
        TaskListItemComponent,
        TaskInlineComponent,
        TaskListComponent],
})
export class ProjectTasksModule {}
