import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { TaskListItemComponent } from './components/task-list-item/task-list-item.component';
import { ProjectTasksComponent } from './index/project-tasks.index.component';
import { ProjectTasksRoutingModule } from './project-tasks-routing.module';
import { ProjectTasksEffects } from './store/tasks.effects';
import { projectTasksReducer } from './store/tasks.reducer';
import { TaskListGroupComponent } from './components/task-list-group/task-list-group.component';
import { TaskDetailComponent } from './components/task-detail/task-detail.component';
import { StaticModule } from '@app/static/static.module';

@NgModule({
  declarations: [
    ProjectTasksComponent,
    TaskListGroupComponent,
    TaskListItemComponent,
    TaskDetailComponent,
  ],
  imports: [
    SharedModule,
    StaticModule,
    StoreModule.forFeature('tasks', projectTasksReducer),
    EffectsModule.forFeature([ProjectTasksEffects]),
    ProjectTasksRoutingModule,
  ],
})
export class ProjectTasksModule {}
