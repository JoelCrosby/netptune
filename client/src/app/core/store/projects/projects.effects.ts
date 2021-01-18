import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectCurrentProject } from '@core/store/projects/projects.selectors';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import {
  catchError,
  map,
  switchMap,
  tap,
  throttleTime,
  withLatestFrom,
} from 'rxjs/operators';
import { selectWorkspace } from '../workspaces/workspaces.actions';
import * as actions from './projects.actions';
import { ProjectsService } from './projects.service';

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
            catchError((error) => of(actions.loadProjectDetailFail({ error })))
          )
        )
      )
  );

  loadProjects$ = createEffect(
    ({ throttle = 200, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.loadProjects, selectWorkspace),
        throttleTime(throttle, scheduler),
        switchMap(() =>
          this.projectsService.get().pipe(
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
      switchMap(({ project }) =>
        this.confirmation.open(DELETE_PROJECT_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.projectsService.delete(project).pipe(
              tap(() => this.snackbar.open('Project Deleted')),
              map((response) =>
                actions.deleteProjectSuccess({
                  response,
                  projectId: project.id,
                })
              ),
              catchError((error) => of(actions.deleteProjectFail({ error })))
            );
          })
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
