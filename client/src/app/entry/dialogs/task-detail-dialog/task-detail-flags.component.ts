import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { loadTaskDetails } from '@app/core/store/tasks/tasks.actions';
import {
  FlagResolutionType,
  ProjectTasksService,
} from '@app/core/store/tasks/tasks.service';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { SnackbarService } from '@app/static/components/snackbar/snackbar.service';
import { LucideCheck, LucideFlag, LucideX } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-task-detail-flags',
  imports: [LucideCheck, LucideFlag, LucideX],
  template: `
    @if (task(); as task) {
      @if (task.flags.length) {
        <section class="mt-5" aria-labelledby="task-flags-heading">
          <div class="mb-2 flex items-center gap-2">
            <svg
              lucideFlag
              size="17"
              class="text-amber-600"
              aria-hidden="true"></svg>
            <h3
              id="task-flags-heading"
              class="text-foreground text-sm font-semibold">
              Flags
            </h3>
            <span class="text-muted text-xs">{{ task.flags.length }}</span>
          </div>

          <div class="flex flex-col gap-2">
            @for (flag of task.flags; track flag.id) {
              <article
                class="rounded-lg border border-amber-400/40 bg-amber-400/8 p-3">
                <div class="flex items-start justify-between gap-4">
                  <div class="min-w-0">
                    <p class="text-sm font-medium text-amber-800">
                      {{ flag.name }}
                    </p>
                    @if (flag.description) {
                      <p class="text-muted mt-1 text-sm">
                        {{ flag.description }}
                      </p>
                    }
                    @if (flag.automationRuleId) {
                      <p class="text-muted mt-1 text-xs">
                        Added by automation rule #{{ flag.automationRuleId }}
                      </p>
                    }
                  </div>

                  @if (canResolve()) {
                    <div class="flex shrink-0 items-center gap-1">
                      <button
                        type="button"
                        class="hover:bg-foreground/5 flex h-8 items-center gap-1 rounded px-2 text-xs font-medium text-green-700 disabled:opacity-50"
                        [disabled]="resolvingFlagId() === flag.id"
                        (click)="
                          resolve(
                            task.id,
                            task.systemId,
                            flag.id,
                            resolutionType.resolved
                          )
                        ">
                        <svg lucideCheck size="14" aria-hidden="true"></svg>
                        Resolve
                      </button>
                      <button
                        type="button"
                        class="text-muted hover:bg-foreground/5 flex h-8 items-center gap-1 rounded px-2 text-xs font-medium disabled:opacity-50"
                        [disabled]="resolvingFlagId() === flag.id"
                        (click)="
                          resolve(
                            task.id,
                            task.systemId,
                            flag.id,
                            resolutionType.dismissed
                          )
                        ">
                        <svg lucideX size="14" aria-hidden="true"></svg>
                        Dismiss
                      </button>
                    </div>
                  }
                </div>
              </article>
            }
          </div>
        </section>
      }
    }
  `,
})
export class TaskDetailFlagsComponent {
  private readonly store = inject(Store);
  private readonly service = inject(ProjectTasksService);
  private readonly snackbar = inject(SnackbarService);
  private readonly destroyRef = inject(DestroyRef);

  readonly resolutionType = FlagResolutionType;
  readonly task = this.store.selectSignal(selectDetailTask);
  readonly resolvingFlagId = signal<number | null>(null);
  readonly canResolve = this.store.selectSignal(
    selectHasPermission(netptunePermissions.flags.resolve)
  );

  resolve(
    taskId: number,
    systemId: string,
    flagId: number,
    resolution: FlagResolutionType
  ) {
    this.resolvingFlagId.set(flagId);
    this.service
      .resolveFlag(taskId, flagId, resolution)
      .pipe(
        finalize(() => this.resolvingFlagId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          const message =
            resolution === FlagResolutionType.resolved
              ? 'Flag resolved'
              : 'Flag dismissed';
          this.snackbar.open(message);
          this.store.dispatch(loadTaskDetails.init({ systemId }));
        },
        error: () => this.snackbar.error('Flag could not be updated'),
      });
  }
}
