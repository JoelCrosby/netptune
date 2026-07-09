import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectCurrentProject } from '@core/store/projects/projects.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { Action, Store } from '@ngrx/store';
import { asyncScheduler, EMPTY, of } from 'rxjs';
import {
  catchError,
  debounceTime,
  map,
  switchMap,
  tap,
  throttleTime,
} from 'rxjs/operators';
import { selectWorkspace } from '../workspaces/workspaces.actions';
import * as actions from './projects.actions';
import { ProjectsService } from './projects.service';

@Injectable()
export class ProjectsEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private projectsService = inject(ProjectsService);
  private confirmation = inject(ConfirmationService);
  private store = inject(Store);
  private snackbar = inject(SnackbarService);

  loadProjects$ = createEffect(
    ({ throttle = 200, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.loadProjects.init, selectWorkspace),
        throttleTime(throttle, scheduler),
        switchMap(() =>
          this.projectsService.get().pipe(
            map((projects) => actions.loadProjects.success({ projects })),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadProjects.fail({ error }))
            )
          )
        )
      );
    }
  );

  loadProjectsSuccess$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadProjects.success),
      concatLatestFrom(() => this.store.select(selectCurrentProject)),
      map(([action, project]) => {
        if (project) return { type: '[N/A]' };
        return actions.selectProject({
          project: action.projects && action.projects[0],
        });
      })
    );
  });

  createProject$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createProject.init),
      switchMap((action) =>
        this.projectsService.post(action.project).pipe(
          unwrapClientReposne(),
          map((project) => actions.createProject.success({ project })),
          catchError((error: HttpErrorResponse) =>
            of(actions.createProject.fail({ error }))
          )
        )
      )
    );
  });

  deleteProject$ = createEffect(
    ({ debounce = 200, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.deleteProject.init),
        switchMap(({ project }) =>
          this.confirmation.open(DELETE_PROJECT_CONFIRMATION).pipe(
            switchMap((result) => {
              if (!result) return EMPTY;

              return this.projectsService.delete(project).pipe(
                debounceTime(debounce, scheduler),
                unwrapClientReposne(),
                tap(() => this.snackbar.open('Project deleted')),
                map(() =>
                  actions.deleteProject.success({
                    projectId: project.id,
                  })
                ),
                catchError((error: HttpErrorResponse) =>
                  of(actions.deleteProject.fail({ error }))
                )
              );
            })
          )
        )
      );
    }
  );

  updateProject$ = createEffect(
    ({ debounce = 400, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.updateProject.init),
        debounceTime(debounce, scheduler),
        switchMap((action) =>
          this.projectsService.put(action.project).pipe(
            unwrapClientReposne(),
            tap(() => this.snackbar.open('Project updated')),
            map((project) => actions.updateProject.success({ project })),
            catchError((error: HttpErrorResponse) =>
              of(actions.updateProject.fail({ error }))
            )
          )
        )
      );
    }
  );

  getProjectBoards$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.getProjectBoards.init),
      switchMap((action) =>
        this.projectsService.getProjectBoards(action.projectId).pipe(
          map((boards) => actions.getProjectBoards.success({ boards })),
          catchError((error: HttpErrorResponse) =>
            of(actions.getProjectBoards.fail({ error }))
          )
        )
      )
    );
  });

  onWorkspaceSelected$ = createEffect(() => {
    return this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState));
  });
}

const DELETE_PROJECT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete Project',
  cancelLabel: 'Cancel',
  color: 'warn',
  title: 'Delete Project',
  message: 'Are you sure you wish to delete this project',
};
