import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmationService } from '@app/core/services/confirmation.service';
import { ConfirmDialogOptions } from '@app/entry/dialogs/confirm-dialog/confirm-dialog.component';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
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
import * as actions from './tasks.actions';
import { ProjectTasksService } from './tasks.service';

@Injectable()
export class ProjectTasksEffects {
  loadProjectTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadProjectTasks),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([action, workspace]) =>
        this.projectTasksService.get(workspace.slug).pipe(
          map((tasks) => actions.loadProjectTasksSuccess({ tasks })),
          catchError((error) => of(actions.loadProjectTasksFail(error)))
        )
      )
    )
  );

  createProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createProjectTask),
      switchMap((action) =>
        this.projectTasksService.post(action.task).pipe(
          tap(() => this.snackbar.open('Task created')),
          map((task) => actions.createProjectTasksSuccess({ task })),
          catchError((error) => of(actions.createProjectTasksFail({ error })))
        )
      )
    )
  );

  editProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.editProjectTask),
      switchMap((action) =>
        this.projectTasksService.put(action.task).pipe(
          tap(() => !!action.silent && this.snackbar.open('Task updated')),
          map((task) => actions.editProjectTasksSuccess({ task })),
          catchError((error) => of(actions.editProjectTasksFail({ error })))
        )
      )
    )
  );

  deleteProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteProjectTask),
      switchMap((action) =>
        this.confirmation.open(DELETE_TASK_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.projectTasksService.delete(action.task).pipe(
              tap(() => this.snackbar.open('Task deleted')),
              map((task) => actions.deleteProjectTasksSuccess({ task })),
              catchError((error) =>
                of(actions.deleteProjectTasksFail({ error }))
              )
            );
          })
        )
      )
    )
  );

  loadTaskDetail$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadTaskDetails),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([action, workspace]) =>
        this.projectTasksService.detail(action.systemId, workspace.slug).pipe(
          map((task) => actions.loadTaskDetailsSuccess({ task })),
          catchError((error) => of(actions.loadTaskDetailsFail({ error })))
        )
      )
    )
  );

  loadComments$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadComments),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([action, workspace]) =>
        this.projectTasksService
          .getComments(action.systemId, workspace.slug)
          .pipe(
            map((comments) => actions.loadCommentsSuccess({ comments })),
            catchError((error) => of(actions.loadCommentsFail({ error })))
          )
      )
    )
  );

  addComment$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.addComment),
      switchMap((action) =>
        this.projectTasksService.postComment(action.request).pipe(
          map((comment) => actions.addCommentSuccess({ comment })),
          catchError((error) => of(actions.addCommentFail({ error })))
        )
      )
    )
  );

  onWorkspaceSelected$ = createEffect(() =>
    this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState))
  );

  constructor(
    private actions$: Actions<Action>,
    private projectTasksService: ProjectTasksService,
    private confirmation: ConfirmationService,
    private snackbar: MatSnackBar,
    private store: Store
  ) {}
}

const DELETE_TASK_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this task?',
  title: 'Delete Task',
};
