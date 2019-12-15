import { Injectable } from '@angular/core';
import { AppState } from '@core/core.state';
import { SelectCurrentWorkspace } from '@core/workspaces/workspaces.selectors';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap, withLatestFrom } from 'rxjs/operators';
import * as actions from './projects.actions';
import { ProjectsService } from './projects.service';

@Injectable()
export class ProjectsEffects {
  loadProjects$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadProjects),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([action, workspace]) =>
        this.projectsService.get(workspace.slug).pipe(
          map(projects => actions.loadProjectsSuccess({ projects })),
          catchError(error => of(actions.loadProjectsFail({ error })))
        )
      )
    )
  );

  createProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createProject),
      switchMap(action =>
        this.projectsService.post(action.project).pipe(
          map(project => actions.createProjectSuccess({ project })),
          catchError(error => of(actions.createProjectFail({ error })))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private projectsService: ProjectsService,
    private store: Store<AppState>
  ) {}
}
