import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { selectCurrentProject } from '@core/store/projects/projects.selectors';
import { SelectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import {
  catchError,
  map,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import { selectWorkspace } from '../workspaces/workspaces.actions';
import * as actions from './projects.actions';
import { ProjectsService } from './projects.service';

@Injectable()
export class ProjectsEffects {
  loadProjects$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadProjects, selectWorkspace),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([action, workspace]) =>
        this.projectsService.get(workspace.slug).pipe(
          map((projects) => actions.loadProjectsSuccess({ projects })),
          catchError((error) => of(actions.loadProjectsFail({ error })))
        )
      )
    )
  );

  loadProjectsSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadProjectsSuccess),
      withLatestFrom(this.store.select(selectCurrentProject)),
      map(([action, project]) => {
        if (project) return { type: '[N/A]' };
        return actions.selectProject({
          project: action.projects && action.projects[0],
        });
      })
    )
  );

  createProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createProject),
      switchMap((action) =>
        this.projectsService.post(action.project).pipe(
          map((project) => actions.createProjectSuccess({ project })),
          catchError((error) => of(actions.createProjectFail({ error })))
        )
      )
    )
  );

  deleteProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteProject),
      switchMap((action) =>
        this.projectsService.delete(action.project).pipe(
          tap(() => this.snackbar.open('Project Deleted')),
          map((project) => actions.deleteProjectSuccess({ project })),
          catchError((error) => of(actions.deleteProjectFail({ error })))
        )
      )
    )
  );

  onWorkspaceSelected$ = createEffect(() =>
    this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState))
  );

  constructor(
    private actions$: Actions<Action>,
    private projectsService: ProjectsService,
    private store: Store,
    private snackbar: MatSnackBar
  ) {}
}
