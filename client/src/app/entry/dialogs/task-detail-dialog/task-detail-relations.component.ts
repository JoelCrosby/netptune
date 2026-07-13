import { DialogRef } from '@angular/cdk/dialog';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, linkedSignal } from '@angular/core';
import { Router } from '@angular/router';
import { selectCanUpdateTask } from '@app/core/store/permissions/permissions.selectors';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { TaskRelation } from '@core/models/task-relation';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { DialogService } from '@core/services/dialog.service';
import { TaskRelationsService } from '@core/services/task-relations.service';
import { selectCurrentHubGroupId } from '@core/store/hub-context/hub-context.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { LucideLink2, LucidePlus, LucideX } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { EMPTY, catchError, concatMap, from, tap, toArray } from 'rxjs';
import {
  LinkTaskDialogComponent,
  LinkTaskDialogData,
  LinkTaskDialogResult,
} from '../link-task-dialog/link-task-dialog.component';

interface RelationGroup {
  label: string;
  relations: TaskRelation[];
}

@Component({
  selector: 'app-task-detail-relations',
  imports: [
    IconButtonComponent,
    StrokedButtonComponent,
    TaskScopeIdComponent,
    TooltipDirective,
    LucideLink2,
    LucidePlus,
    LucideX,
  ],
  template: `
    <div class="mt-4 mb-2 flex items-center justify-between">
      <h4 class="font-sm font-semibold">Relations</h4>
      @if (canUpdate()) {
        <button
          app-stroked-button
          type="button"
          size="sm"
          (click)="openLinkDialog()">
          <svg lucidePlus class="h-4 w-4"></svg>
          <span>Link task</span>
        </button>
      }
    </div>

    @if (error()) {
      <div class="text-danger mb-2 text-sm">{{ error() }}</div>
    }

    @for (group of groups(); track group.label) {
      <div class="mb-3">
        <div
          class="text-muted-foreground mb-1 text-xs font-medium tracking-wide uppercase">
          {{ group.label }}
        </div>

        <ul class="flex flex-col gap-1">
          @for (relation of group.relations; track relation.id) {
            <li
              class="border-border bg-card flex items-center gap-3 rounded border px-3 py-2">
              <span
                class="h-2 w-2 shrink-0 rounded-full"
                [style.background-color]="
                  relation.relatedTask.statusColor ?? '#64748b'
                "></span>

              <app-task-scope-id [id]="relation.relatedTask.systemId" />

              <button
                type="button"
                class="flex-1 cursor-pointer truncate text-left"
                (click)="openTask(relation)">
                {{ relation.relatedTask.name }}
              </button>

              <span class="text-muted-foreground shrink-0 text-xs">
                {{ relation.relatedTask.statusName }}
              </span>

              @if (canUpdate()) {
                <button
                  app-icon-button
                  appTooltip="Remove link"
                  aria-label="Remove link"
                  [disabled]="busy()"
                  (click)="unlink(relation)">
                  <svg lucideX class="h-4 w-4"></svg>
                </button>
              }
            </li>
          }
        </ul>
      </div>
    } @empty {
      <div class="text-muted-foreground flex items-center gap-2 text-sm">
        <svg lucideLink2 class="h-4 w-4"></svg>
        <span>No linked tasks</span>
      </div>
    }
  `,
})
export class TaskDetailRelationsComponent {
  private readonly store = inject(Store);
  private readonly relationsService = inject(TaskRelationsService);
  private readonly dialog = inject(DialogService);
  private readonly snackbar = inject(SnackbarService);
  private readonly router = inject(Router);

  // Present when this renders inside the task detail dialog, absent on the standalone task page.
  private readonly dialogRef = inject(DialogRef, { optional: true });

  readonly task = this.store.selectSignal(selectDetailTask);
  readonly hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);
  readonly canUpdate = selectCanUpdateTask(this.store);

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

  // httpResource blanks its value while a new request is in flight, which would tear the section
  // down and rebuild it every time the user clicks through to a linked task. Holding the previous
  // list keeps the rendered rows in place until the new ones arrive.
  private readonly displayedRelations = linkedSignal<
    TaskRelation[],
    TaskRelation[]
  >({
    source: () => this.relations.value(),
    computation: (next, previous) =>
      this.relations.isLoading() && previous ? previous.value : next,
  });

  // Relations arrive ordered by relation type and direction, so consecutive rows sharing a label
  // belong together. Grouping by label rather than by type id is deliberate: one type produces two
  // groups ("Blocks" and "Is Blocked By") depending on which end this task sits on.
  readonly groups = computed<RelationGroup[]>(() => {
    const groups: RelationGroup[] = [];

    for (const relation of this.displayedRelations()) {
      const last = groups.at(-1);

      if (last?.label === relation.label) {
        last.relations.push(relation);
        continue;
      }

      groups.push({ label: relation.label, relations: [relation] });
    }

    return groups;
  });

  openLinkDialog() {
    const task = this.task();

    if (!task) return;

    const dialogRef = this.dialog.open<
      LinkTaskDialogResult,
      LinkTaskDialogData
    >(LinkTaskDialogComponent, {
      data: { task },
      width: '900px',
    });

    dialogRef.closed.subscribe((result) => {
      if (!result) return;

      this.link(task, result);
    });
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
            .create(
              { ...request, relationTypeId: result.relationTypeId },
              this.hubGroupId()
            )
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

  unlink(relation: TaskRelation) {
    this.busy.set(true);
    this.error.set(null);

    this.relationsService
      .delete(relation.id, this.hubGroupId())
      .pipe(
        unwrapClientReposne(),
        tap(() => {
          this.busy.set(false);
          this.relations.reload();
          this.snackbar.open('Link removed');
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
  openTask(relation: TaskRelation) {
    const workspaceKey = this.task()?.workspaceKey;

    if (!workspaceKey) return;

    this.dialogRef?.close();

    void this.router.navigate([
      '/',
      workspaceKey,
      'tasks',
      relation.relatedTask.systemId,
    ]);
  }
}
