import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, map, switchMap, withLatestFrom } from 'rxjs/operators';
import {
  ActionLoadProjectTasksFail,
  ActionLoadProjectTasksSuccess,
  ProjectTasksActions,
  ProjectTasksActionTypes,
  ActionEditProjectTasksSuccess,
  ActionEditProjectTasksFail,
  ActionDeleteProjectTasksSuccess,
  ActionDeleteProjectTasksFail,
  ActionCreateProjectTasksSuccess,
  ActionCreateProjectTasksFail,
} from './project-tasks.actions';
import { ProjectTasksService } from './project-tasks.service';
import { SelectCurrentWorkspace } from '@app/core/state/core.selectors';
import { AppState } from '@app/core/core.state';
import { Store } from '@ngrx/store';
import { Update } from '@ngrx/entity';

@Injectable()
export class ProjectTasksEffects {
  constructor(
    private actions$: Actions<ProjectTasksActions>,
    private projectTasksService: ProjectTasksService,
    private store: Store<AppState>
  ) {}

  @Effect()
  loadProjectTasks$ = this.actions$.pipe(
    ofType(ProjectTasksActionTypes.LoadProjectTasks),
    withLatestFrom(this.store.select(SelectCurrentWorkspace)),
    switchMap(([action, workspace]) =>
      this.projectTasksService.get(workspace).pipe(
        map(tasks => new ActionLoadProjectTasksSuccess(tasks)),
        catchError(error => of(new ActionLoadProjectTasksFail(error)))
      )
    )
  );

  @Effect()
  createProjectTask$ = this.actions$.pipe(
    ofType(ProjectTasksActionTypes.CreateProjectTask),
    switchMap(action =>
      this.projectTasksService.post(action.payload).pipe(
        map(task => new ActionCreateProjectTasksSuccess(task)),
        catchError(error => of(new ActionCreateProjectTasksFail(error)))
      )
    )
  );

  @Effect()
  editProjectTask$ = this.actions$.pipe(
    ofType(ProjectTasksActionTypes.EditProjectTask),
    switchMap(action =>
      this.projectTasksService.put(action.payload).pipe(
        map(task => new ActionEditProjectTasksSuccess(task)),
        catchError(error => of(new ActionEditProjectTasksFail(error)))
      )
    )
  );

  @Effect()
  deleteProjectTask$ = this.actions$.pipe(
    ofType(ProjectTasksActionTypes.DeleteProjectTask),
    switchMap(action =>
      this.projectTasksService.delete(action.payload).pipe(
        map(task => new ActionDeleteProjectTasksSuccess(task)),
        catchError(error => of(new ActionDeleteProjectTasksFail(error)))
      )
    )
  );
}
