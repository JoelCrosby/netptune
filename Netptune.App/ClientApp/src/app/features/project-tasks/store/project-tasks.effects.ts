import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import {
  ActionLoadProjectTasksFail,
  ActionLoadProjectTasksSuccess,
  ProjectTasksActions,
  ProjectTasksActionTypes,
} from './project-tasks.actions';
import { ProjectTasksService } from './project-tasks.service';

@Injectable()
export class ProjectTasksEffects {
  constructor(
    private actions$: Actions<ProjectTasksActions>,
    private projectTasksService: ProjectTasksService
  ) {}

  @Effect()
  loadProjectTasks$ = this.actions$.pipe(
    ofType(ProjectTasksActionTypes.LoadProjectTasks),
    switchMap(action =>
      this.projectTasksService.get(1).pipe(
        map(tasks => new ActionLoadProjectTasksSuccess(tasks)),
        catchError(error => of(new ActionLoadProjectTasksFail(error)))
      )
    )
  );
}
