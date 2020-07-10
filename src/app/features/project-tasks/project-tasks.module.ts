import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { TaskListItemComponent } from './components/task-list-item/task-list-item.component';
import { ProjectTasksViewComponent } from './views/project-tasks-view/project-tasks-view.component';
import { ProjectTasksRoutingModule } from './project-tasks-routing.module';
import { ProjectTasksEffects } from '../../core/store/tasks/tasks.effects';
import { projectTasksReducer } from '../../core/store/tasks/tasks.reducer';
import { TaskListGroupComponent } from './components/task-list-group/task-list-group.component';
import { StaticModule } from '@app/static/static.module';
import { TaskInlineComponent } from './components/task-inline/task-inline.component';
import { TaskListComponent } from './components/task-list/task-list.component';

@NgModule({
  declarations: [
    ProjectTasksViewComponent,
    TaskListGroupComponent,
    TaskListItemComponent,
    TaskInlineComponent,
    TaskListComponent,
  ],
  imports: [SharedModule, StaticModule, ProjectTasksRoutingModule],
})
export class ProjectTasksModule {}
