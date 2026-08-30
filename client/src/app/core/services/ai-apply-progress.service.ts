import { Service, computed, signal } from '@angular/core';
import {
  AiApplyProgress,
  AiApplyProgressType,
} from '@core/models/ai-apply-progress';
import { AiChangeApplyStatus } from '@core/models/ai-conversation';

/**
 * What a run has reached so far, change by change. The server settles the change set itself,
 * so this only lives as long as the request that reports it.
 */
@Service()
export class AiApplyProgressService {
  readonly changeSetId = signal<string | null>(null);
  readonly total = signal(0);
  readonly activeChangeId = signal<number | null>(null);
  readonly isStopping = signal(false);
  readonly statuses = signal<ReadonlyMap<number, AiChangeApplyStatus>>(
    new Map()
  );

  readonly completed = computed(() => this.statuses().size);

  readonly percent = computed(() => {
    const total = this.total();

    if (total === 0) {
      return 0;
    }

    return Math.min(100, Math.round((this.completed() / total) * 100));
  });

  start(changeSetId: string, total: number) {
    this.changeSetId.set(changeSetId);
    this.total.set(total);
    this.statuses.set(new Map());
    this.activeChangeId.set(null);
    this.isStopping.set(false);
  }

  reset() {
    this.changeSetId.set(null);
    this.total.set(0);
    this.statuses.set(new Map());
    this.activeChangeId.set(null);
    this.isStopping.set(false);
  }

  isRunning(changeSetId: string): boolean {
    return this.changeSetId() === changeSetId;
  }

  markStopping() {
    this.isStopping.set(true);
  }

  receive(progress: AiApplyProgress) {
    if (progress.type === AiApplyProgressType.started) {
      this.total.set(progress.total);

      return;
    }

    if (progress.type === AiApplyProgressType.changeStarted) {
      this.activeChangeId.set(progress.changeId ?? null);

      return;
    }

    if (progress.type === AiApplyProgressType.changeCompleted) {
      this.record(progress);

      return;
    }

    this.activeChangeId.set(null);
  }

  private record(progress: AiApplyProgress) {
    const changeId = progress.changeId;

    if (changeId === null || changeId === undefined) {
      return;
    }

    this.statuses.update((current) => {
      const next = new Map(current);

      next.set(changeId, progress.status ?? AiChangeApplyStatus.applied);

      return next;
    });

    const isActive = this.activeChangeId() === changeId;

    if (isActive) {
      this.activeChangeId.set(null);
    }
  }
}
