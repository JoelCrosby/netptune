import { Service, Signal, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  AiReviewRunResult,
  AiReviewsService,
} from '@core/services/ai-reviews.service';
import { TASK_DETAIL_SECTION_PARAM } from '@core/tasks/task-detail-section';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import {
  AiReviewLiveRun,
  applyAiReviewEvent,
  startAiReviewLiveRun,
} from '@core/util/ai-review-live-run';
import { refreshScopesForEntityTypes } from '@core/util/entity-refresh-scopes';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';

// What the service needs to name and reopen a task once its run has outlived the
// surface that started it.
export interface TaskReviewSubject {
  id: number;
  systemId: string;
  workspaceKey: string;
}

const FINISHED_SNACKBAR_MS = 8_000;
const FAILED_SNACKBAR_MS = 10_000;

@Service()
export class TaskReviewRunsService {
  private readonly reviews = inject(AiReviewsService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly snackbar = inject(SnackbarService);
  private readonly router = inject(Router);

  private readonly runs = signal<ReadonlyMap<number, AiReviewLiveRun>>(
    new Map()
  );

  // A surface showing a task's review while it runs. Counted, because the dialog
  // and the standalone task page can both be open on the same task.
  private readonly watchers = new Map<number, number>();

  private readonly viewLabel = $localize`:Action on the notice that a task review has finished, which opens the task:Open task`;

  // Reads the running map, so a caller in a reactive context refreshes when a run on
  // any task starts or ends.
  isRunning(taskId: number): boolean {
    return this.runs().has(taskId);
  }

  forTask(taskId: Signal<number | undefined>) {
    return computed(() => {
      const id = taskId();

      return id === undefined ? null : (this.runs().get(id) ?? null);
    });
  }

  // Called while a surface is showing this task's review, so a run that finishes
  // under the reader's eyes does not also raise a notice. Returns the undo.
  watch(taskId: number): () => void {
    this.addWatcher(taskId, 1);

    return () => this.addWatcher(taskId, -1);
  }

  async run(subject: TaskReviewSubject): Promise<AiReviewRunResult | null> {
    const isAlreadyRunning = this.runs().has(subject.id);

    if (isAlreadyRunning) return null;

    this.set(subject.id, startAiReviewLiveRun());

    const result = await this.reviews.runTaskReview(subject.id, (event) => {
      const current = this.runs().get(subject.id);

      if (!current) return;

      this.set(subject.id, applyAiReviewEvent(current, event));
    });

    this.runs.update((runs) => {
      const remaining = new Map(runs);

      remaining.delete(subject.id);

      return remaining;
    });

    this.workspaceRefresh.refresh(refreshScopesForEntityTypes(['aiReview']));
    this.announce(subject, result);

    return result;
  }

  private announce(subject: TaskReviewSubject, result: AiReviewRunResult) {
    const isWatched = (this.watchers.get(subject.id) ?? 0) > 0;

    if (isWatched) return;

    const open = () => this.openReview(subject);

    if (result.failure) {
      this.snackbar.open(failedMessage(subject.systemId), this.viewLabel, {
        type: 'error',
        duration: FAILED_SNACKBAR_MS,
        onAction: open,
      });

      return;
    }

    this.snackbar.open(
      finishedMessage(subject.systemId, result.findingCount),
      this.viewLabel,
      {
        type: 'success',
        duration: FINISHED_SNACKBAR_MS,
        onAction: open,
      }
    );
  }

  private openReview(subject: TaskReviewSubject) {
    void this.router.navigate(
      ['/', subject.workspaceKey, 'tasks', subject.systemId],
      { queryParams: { [TASK_DETAIL_SECTION_PARAM]: 'review' } }
    );
  }

  private addWatcher(taskId: number, delta: number) {
    const next = (this.watchers.get(taskId) ?? 0) + delta;

    if (next > 0) {
      this.watchers.set(taskId, next);
    } else {
      this.watchers.delete(taskId);
    }
  }

  private set(taskId: number, run: AiReviewLiveRun) {
    this.runs.update((runs) => new Map(runs).set(taskId, run));
  }
}

function failedMessage(systemId: string): string {
  return $localize`:Notice shown when a task review failed while the task was not on screen. TASK is a task identifier like NETP-612:The review of ${systemId}:TASK: failed.`;
}

// One whole sentence per message, because $localize cannot evaluate ICU.
function finishedMessage(
  systemId: string,
  findingCount: number | null
): string {
  if (findingCount === null) {
    return $localize`:Notice shown when a task review finished while the task was not on screen, and the number of findings is unknown. TASK is a task identifier like NETP-612:The review of ${systemId}:TASK: has finished.`;
  }

  if (findingCount === 0) {
    return $localize`:Notice shown when a task review finished with no findings while the task was not on screen. TASK is a task identifier like NETP-612:The review of ${systemId}:TASK: found nothing to fix.`;
  }

  if (findingCount === 1) {
    return $localize`:Notice shown when a task review finished with one finding while the task was not on screen. TASK is a task identifier like NETP-612:The review of ${systemId}:TASK: found 1 thing to look at.`;
  }

  return $localize`:Notice shown when a task review finished with several findings while the task was not on screen. TASK is a task identifier like NETP-612, COUNT is a count greater than one:The review of ${systemId}:TASK: found ${findingCount}:COUNT: things to look at.`;
}
