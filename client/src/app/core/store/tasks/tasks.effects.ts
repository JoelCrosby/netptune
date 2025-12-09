import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { downloadFile } from '@core/util/download-helper';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';
import { loadProjects } from '../projects/projects.actions';
import { loadTags } from '../tags/tags.actions';
import { loadUsers } from '../users/users.actions';
import * as actions from './tasks.actions';
import { ProjectTasksHubService } from './tasks.hub.service';
import { ProjectTasksService } from './tasks.service';
import { clearState } from '../activity/activity.actions';

@Injectable()
export class ProjectTasksEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private projectTasksService = inject(ProjectTasksService);
  private projectTasksHubService = inject(ProjectTasksHubService);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(MatSnackBar);

  loadProjectTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadProjectTasks),
      switchMap(() =>
        this.projectTasksService.get().pipe(
          map((tasks) => actions.loadProjectTasksSuccess({ tasks })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadProjectTasksFail(error))
          )
        )
      )
    );
  });

  createProjectTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createProjectTask),
      switchMap((action) =>
        this.projectTasksHubService.post(action.identifier, action.task).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Task created')),
          map((task) => actions.createProjectTasksSuccess({ task })),
          catchError((error: HttpErrorResponse) =>
            of(actions.createProjectTasksFail({ error }))
          )
        )
      )
    );
  });

  editProjectTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.editProjectTask),
      switchMap((action) =>
        this.projectTasksHubService.put(action.identifier, action.task).pipe(
          unwrapClientReposne(),
          tap(() => !!action.silent && this.snackbar.open('Task updated')),
          map((task) => actions.editProjectTasksSuccess({ task })),
          catchError((error: HttpErrorResponse) =>
            of(actions.editProjectTasksFail({ error }))
          )
        )
      )
    );
  });

  deleteProjectTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteProjectTask),
      switchMap((action) =>
        this.confirmation.open(DELETE_TASK_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.projectTasksHubService
              .delete(action.identifier, action.task)
              .pipe(
                unwrapClientReposne(),
                tap(() => this.snackbar.open('Task deleted')),
                map(() => {
                  const taskId = action.task.id;

                  if (taskId === undefined || taskId === null) {
                    throw new Error('taskid was null or undefined');
                  }

                  return actions.deleteProjectTasksSuccess({
                    taskId,
                  });
                }),
                catchError((error) =>
                  of(actions.deleteProjectTasksFail({ error }))
                )
              );
          })
        )
      )
    );
  });

  deleteComment$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteComment),
      switchMap((action) =>
        this.confirmation.open(DELETE_COMMENT_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.projectTasksService
              .deleteComment(action.commentId)
              .pipe(
                unwrapClientReposne(),
                tap(() => this.snackbar.open('Comment deleted')),
                map(() =>
                  actions.deleteCommentSuccess({
                    commentId: action.commentId,
                  })
                ),
                catchError((error: HttpErrorResponse) =>
                  of(actions.deleteCommentFail({ error }))
                )
              );
          })
        )
      )
    );
  });

  loadTaskDetail$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadTaskDetails),
      switchMap((action) =>
        this.projectTasksService.detail(action.systemId).pipe(
          map((task) => actions.loadTaskDetailsSuccess({ task })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadTaskDetailsFail({ error }))
          )
        )
      )
    );
  });

  loadTaskDetailComments$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadTaskDetails),
      map(({ systemId }) => actions.loadComments({ systemId }))
    );
  });

  loadTaskDetailProjects$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadTaskDetails),
      map(() => loadProjects())
    );
  });

  loadTaskDetailUsers$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadTaskDetails),
      map(() => loadUsers())
    );
  });

  loadTaskDetailTags$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadTaskDetails),
      map(() => loadTags())
    );
  });

  clearTaskDetail$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.clearTaskDetail),
      map(() => clearState())
    );
  });

  loadComments$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadComments),
      switchMap((action) =>
        this.projectTasksService.getComments(action.systemId).pipe(
          map((comments) => actions.loadCommentsSuccess({ comments })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadCommentsFail({ error }))
          )
        )
      )
    );
  });

  addComment$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.addComment),
      switchMap((action) =>
        this.projectTasksService.postComment(action.request).pipe(
          unwrapClientReposne(),
          map((comment) => actions.addCommentSuccess({ comment })),
          catchError((error: HttpErrorResponse) =>
            of(actions.addCommentFail({ error }))
          )
        )
      )
    );
  });

  exportTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.exportTasks),
      switchMap(() =>
        this.projectTasksService.export().pipe(
          tap((res) => void downloadFile(res.file, res.filename)),
          map((reponse) => actions.exportTasksSuccess({ reponse })),
          catchError((error: HttpErrorResponse) =>
            of(actions.exportTasksFail({ error }))
          )
        )
      )
    );
  });

  importTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.importTasks),
      switchMap((action) =>
        this.projectTasksService
          .import(action.boardIdentifier, action.file)
          .pipe(
            unwrapClientReposne(),
            tap(() => this.snackbar.open('Import Successful')),
            map(() => actions.importTasksSuccess()),
            catchError((error) => {
              this.snackbar.open('Import Failed');
              return of(actions.importTasksFail({ error }));
            })
          )
      )
    );
  });

  addTagToTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.addTagToTask),
      switchMap(({ identifier, request }) =>
        this.projectTasksHubService.addTagToTask(identifier, request).pipe(
          unwrapClientReposne(),
          map((tag) => actions.addTagToTaskSuccess({ tag })),
          catchError((error: HttpErrorResponse) =>
            of(actions.addTagToTaskFail(error))
          )
        )
      )
    );
  });

  deleteTagFromTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteTagFromTask),
      switchMap(({ identifier, systemId, tag }) =>
        this.projectTasksHubService
          .deleteTagFromTask(identifier, { systemId, tag })
          .pipe(
            unwrapClientReposne(),
            map(() => actions.deleteTagFromTaskSuccess()),
            catchError((error: HttpErrorResponse) =>
              of(actions.deleteTagFromTaskFail(error))
            )
          )
      )
    );
  });

  onWorkspaceSelected$ = createEffect(() => {
    return this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState));
  });
}

const DELETE_TASK_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this task?',
  title: 'Delete Task',
  color: 'warn',
};

const DELETE_COMMENT_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this comment?',
  title: 'Delete Comment',
  color: 'warn',
};
