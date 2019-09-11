import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { of, throwError } from 'rxjs';
import { catchError, map, switchMap, tap, withLatestFrom } from 'rxjs/operators';
import {
  ActionLoadProjectsSuccess,
  ProjectsActions,
  ProjectsActionTypes,
  ActionLoadProjectsFail,
  ActionCreateProjectFail,
  ActionCreateProjectSuccess,
} from './projects.actions';
import { ProjectsService } from './projects.service';
import { Store } from '@ngrx/store';
import { AppState } from '@app/core/core.state';
import { SelectCurrentWorkspace } from '@app/core/state/core.selectors';

@Injectable()
export class ProjectsEffects {
  constructor(private actions$: Actions<ProjectsActions>, private projectsService: ProjectsService, private store: Store<AppState>) {}

  @Effect()
  loadProjects$ = this.actions$.pipe(
    ofType(ProjectsActionTypes.LoadProjects),
    withLatestFrom(this.store.select(SelectCurrentWorkspace)),
    switchMap(([action, workspace]) =>
      this.projectsService.get(workspace).pipe(
        map(projects => new ActionLoadProjectsSuccess(projects)),
        catchError(error => of(new ActionLoadProjectsFail(error)))
      )
    )
  );

  @Effect()
  createProject$ = this.actions$.pipe(
    ofType(ProjectsActionTypes.CreateProject),
    switchMap(action =>
      this.projectsService.post(action.payload).pipe(
        map(project => new ActionCreateProjectSuccess(project)),
        catchError(error => of(new ActionCreateProjectFail(error)))
      )
    )
  );
}
