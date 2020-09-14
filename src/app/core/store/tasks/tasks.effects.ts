import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { downloadFile } from '@core/util/download-helper';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
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
import { ProjectTasksHubService } from './tasks.hub.service';
import { ProjectTasksService } from './tasks.service';

@Injectable()
export class ProjectTasksEffects {
  loadProjectTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadProjectTasks),
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
      switchMap(([_, workspace]) =>
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
        this.projectTasksHubService.post(action.identifier, action.task).pipe(
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
        this.projectTasksHubService.put(action.identifier, action.task).pipe(
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

            return this.projectTasksHubService
              .delete(action.identifier, action.task)
              .pipe(
                tap(() => this.snackbar.open('Task deleted')),
                map((response) =>
                  actions.deleteProjectTasksSuccess({
                    response,
                    taskId: action.task.id,
                  })
                ),
                catchError((error) =>
                  of(actions.deleteProjectTasksFail({ error }))
                )
              );
          })
        )
      )
    )
  );

  deleteComment$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteComment),
      switchMap((action) =>
        this.confirmation.open(DELETE_COMMENT_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.projectTasksService
              .deleteComment(action.commentId)
              .pipe(
                tap(() => this.snackbar.open('Comment deleted')),
                map((response) =>
                  actions.deleteCommentSuccess({
                    response,
                    commentId: action.commentId,
                  })
                ),
                catchError((error) => of(actions.deleteCommentFail({ error })))
              );
          })
        )
      )
    )
  );

  loadTaskDetail$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadTaskDetails),
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
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
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
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

  exportTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.exportTasks),
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
      switchMap(([_, workspace]) =>
        this.projectTasksService.export(workspace.slug).pipe(
          tap(async (res) => await downloadFile(res.file, res.filename)),
          map((reponse) => actions.exportTasksSuccess({ reponse })),
          catchError((error) => of(actions.exportTasksFail({ error })))
        )
      )
    )
  );

  importTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.importTasks),
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
      switchMap(([action]) =>
        this.projectTasksService
          .import(action.boardIdentifier, action.file)
          .pipe(
            map((reponse) => actions.importTasksSuccess({ reponse })),
            tap((response) => {
              if (response?.reponse?.isSuccess) {
                this.snackbar.open('Import Successful');
              } else {
                this.snackbar.open('Import Failed');
              }
            }),
            catchError((error) => {
              this.snackbar.open('Import Failed');
              return of(actions.importTasksFail({ error }));
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
    private projectTasksService: ProjectTasksService,
    private projectTasksHubService: ProjectTasksHubService,
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

const DELETE_COMMENT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this comment?',
  title: 'Delete Comment',
};
