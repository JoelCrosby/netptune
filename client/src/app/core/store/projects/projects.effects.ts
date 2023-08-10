import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectCurrentProject } from '@core/store/projects/projects.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import {
  catchError,
  debounceTime,
  map,
  switchMap,
  tap,
  throttleTime,
  withLatestFrom,
} from 'rxjs/operators';
import { selectWorkspace } from '../workspaces/workspaces.actions';
import { selectCurrentWorkspaceIdentifier } from '../workspaces/workspaces.selectors';
import * as actions from './projects.actions';
import { ProjectsService } from './projects.service';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable()
export class ProjectsEffects {
  loadProjectDetail$ = createEffect(
    ({ throttle = 200, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.loadProjectDetail),
        throttleTime(throttle, scheduler),
        switchMap((action) =>
          this.projectsService.getProjectDetail(action.projectKey).pipe(
            map((project) => actions.loadProjectDetailSuccess({ project })),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadProjectDetailFail({ error }))
            )
          )
        )
      )
  );

  loadProjectDetailfail$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(actions.loadProjectDetailFail),
        withLatestFrom(this.store.select(selectCurrentWorkspaceIdentifier)),
        tap(
          ([_, workspaceId]) =>
            void this.router.navigate(['/', workspaceId, 'projects'])
        )
      ),
    { dispatch: false }
  );

  loadProjects$ = createEffect(
    ({ throttle = 200, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.loadProjects, selectWorkspace),
        throttleTime(throttle, scheduler),
        switchMap(() =>
          this.projectsService.get().pipe(
            map((projects) => actions.loadProjectsSuccess({ projects })),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadProjectsFail({ error }))
            )
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
          unwrapClientReposne(),
          map((project) => actions.createProjectSuccess({ project })),
          catchError((error: HttpErrorResponse) =>
            of(actions.createProjectFail({ error }))
          )
        )
      )
    )
  );

  deleteProject$ = createEffect(
    ({ debounce = 200, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.deleteProject),
        switchMap(({ project }) =>
          this.confirmation.open(DELETE_PROJECT_CONFIRMATION).pipe(
            switchMap((result) => {
              if (!result) return of({ type: 'NO_ACTION' });

              return this.projectsService.delete(project).pipe(
                debounceTime(debounce, scheduler),
                unwrapClientReposne(),
                tap(() => this.snackbar.open('Project deleted')),
                map(() =>
                  actions.deleteProjectSuccess({
                    projectId: project.id,
                  })
                ),
                catchError((error: HttpErrorResponse) =>
                  of(actions.deleteProjectFail({ error }))
                )
              );
            })
          )
        )
      )
  );

  updateProject$ = createEffect(
    ({ debounce = 400, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.updateProject),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.projectsService.put(action.project).pipe(
            unwrapClientReposne(),
            tap(() => this.snackbar.open('Project updated')),
            map((project) => actions.updateProjectSuccess({ project })),
            catchError((error: HttpErrorResponse) =>
              of(actions.updateProjectFail({ error }))
            )
          )
        )
      )
  );

  getProjectBoards$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.getProjectBoards),
      switchMap((action) =>
        this.projectsService.getProjectBoards(action.projectId).pipe(
          map((boards) => actions.getProjectBoardsSuccess({ boards })),
          catchError((error: HttpErrorResponse) =>
            of(actions.getProjectBoardsFail({ error }))
          )
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
    private confirmation: ConfirmationService,
    private store: Store,
    private router: Router,
    private snackbar: MatSnackBar
  ) {}
}

const DELETE_PROJECT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete Project',
  cancelLabel: 'Cancel',
  color: 'warn',
  title: 'Delete Project',
  message: 'Are you sure you wish to delete this project',
};
