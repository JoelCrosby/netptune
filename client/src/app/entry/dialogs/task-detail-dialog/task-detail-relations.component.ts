import { DialogRef } from '@angular/cdk/dialog';
import { httpResource } from '@angular/common/http';
import {
  Component,
  computed,
  inject,
  input,
  linkedSignal,
} from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { hasPermission } from '@core/auth/has-permission';
import { Router } from '@angular/router';
import { TaskRelation } from '@core/models/task-relation';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { DialogService } from '@core/services/dialog.service';
import { TaskRelationsService } from '@core/services/task-relations.service';
import { retainWhileLoading } from '@core/resources/stable.resource';
import { reloadOnRefresh } from '@core/util/reload-on-refresh';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { LucidePlus } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { EMPTY, catchError, concatMap, from, tap, toArray } from 'rxjs';
import {
  LinkTaskDialogComponent,
  LinkTaskDialogData,
  LinkTaskDialogResult,
} from '../link-task-dialog/link-task-dialog.component';
import {
  TaskRelationListComponent,
  TaskRelationListItem,
} from './parts/task-relation-list.component';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-relations',
  imports: [LucidePlus, StrokedButtonComponent, TaskRelationListComponent],
  template: `
    @if (showHeading()) {
      <div class="mt-4 mb-2 flex items-center justify-between">
        <h4 class="font-sm font-semibold">
          <span i18n="Section heading for links between tasks">Relations</span>
        </h4>
        @if (canUpdate()) {
          <button
            app-stroked-button
            type="button"
            size="small"
            (click)="openLinkDialog()">
            <svg lucidePlus class="h-4 w-4"></svg>
            <span i18n="Button that links this task to another">Link task</span>
          </button>
        }
      </div>
    }

    @if (error()) {
      <div class="text-danger mb-2 text-sm">{{ error() }}</div>
    }

    <app-task-relation-list
      openable
      [items]="items()"
      [removable]="canUpdate()"
      [disabled]="busy()"
      (opened)="openTask($event)"
      (removed)="unlink($event)" />
  `,
})
export class TaskDetailRelationsComponent {
  readonly showHeading = input(false);

  private readonly relationsService = inject(TaskRelationsService);
  private readonly dialog = inject(DialogService);
  private readonly snackbar = inject(SnackbarService);
  private readonly router = inject(Router);

  // Present when this renders inside the task detail dialog, absent on the standalone task page.
  private readonly dialogRef = inject(DialogRef, { optional: true });

  private readonly taskDetail = inject(TaskDetailService);

  readonly task = this.taskDetail.task;
  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);

  readonly busy = linkedSignal({
    source: () => this.task()?.systemId,
    computation: () => false,
  });

  readonly error = linkedSignal<string | undefined, string | null>({
    source: () => this.task()?.systemId,
    computation: () => null,
  });

  private readonly relations = httpResource<TaskRelation[]>(
    () => {
      const systemId = this.task()?.systemId;

      if (!systemId) return undefined;

      return { url: `api/task-relations/${systemId}` };
    },
    { defaultValue: [] }
  );

  private readonly displayedRelations = retainWhileLoading(this.relations);

  readonly count = computed(() => this.displayedRelations().length);

  readonly items = computed<TaskRelationListItem[]>(() => {
    return this.displayedRelations().map((relation) => ({
      id: String(relation.id),
      label: relation.label,
      systemId: relation.relatedTask.systemId,
      name: relation.relatedTask.name,
      statusName: relation.relatedTask.statusName,
      statusColor: relation.relatedTask.statusColor,
    }));
  });

  constructor() {
    reloadOnRefresh(this.relations, ['tasks']);
  }

  async openLinkDialog() {
    const task = this.task();

    if (!task) return;

    const result = await this.dialog.openForResult<
      LinkTaskDialogResult,
      LinkTaskDialogData
    >(LinkTaskDialogComponent, {
      data: { task },
      width: '1100px',
      panelClass: LinkTaskDialogComponent.panelClass,
    });

    if (!result) return;

    this.link(task, result);
  }

  private link(task: TaskViewModel, result: LinkTaskDialogResult) {
    this.busy.set(true);
    this.error.set(null);

    const failures: string[] = [];

    // Sequential rather than parallel: the cycle and single-parent checks each read the rows the
    // previous insert wrote, so concurrent inserts could both pass a check that only one should.
    from(result.tasks)
      .pipe(
        concatMap((other) => {
          const request = result.isForward
            ? { sourceSystemId: task.systemId, targetSystemId: other.systemId }
            : { sourceSystemId: other.systemId, targetSystemId: task.systemId };

          return this.relationsService
            .create({ ...request, relationTypeId: result.relationTypeId })
            .pipe(
              tap((response) => {
                if (!response.isSuccess) {
                  failures.push(
                    `${other.systemId}: ${response.message ?? 'could not be linked'}`
                  );
                }
              }),
              catchError(() => {
                failures.push(`${other.systemId}: could not be linked`);
                return EMPTY;
              })
            );
        }),
        toArray()
      )
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.relations.reload();

          if (failures.length > 0) {
            this.error.set(failures.join(' · '));
            return;
          }

          this.snackbar.open(
            result.tasks.length === 1 ? 'Task linked' : 'Tasks linked'
          );
        },
        error: () => {
          this.busy.set(false);
          this.error.set('Tasks could not be linked.');
        },
      });
  }

  unlink(item: TaskRelationListItem) {
    const relation = this.findRelation(item);

    if (!relation) return;

    this.busy.set(true);
    this.error.set(null);

    this.relationsService
      .delete(relation.id)
      .pipe(
        unwrapClientResponse(),
        tap(() => {
          this.busy.set(false);
          this.relations.reload();
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Link removed`
          );
        }),
        catchError(() => {
          this.busy.set(false);
          this.error.set('Link could not be removed.');
          return EMPTY;
        })
      )
      .subscribe();
  }

  // This section renders inside both the task detail dialog and the standalone task page, so it
  // cannot assume a dialog is open. When one is, it has to close before navigating or it would sit
  // over the task we just navigated to.
  openTask(item: TaskRelationListItem) {
    const workspaceKey = this.task()?.workspaceKey;

    if (!workspaceKey) return;

    this.dialogRef?.close();

    void this.router.navigate(['/', workspaceKey, 'tasks', item.systemId]);
  }

  private findRelation(item: TaskRelationListItem) {
    return this.displayedRelations().find((relation) => {
      return String(relation.id) === item.id;
    });
  }
}
