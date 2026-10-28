import { Service, Signal, computed, inject, signal } from '@angular/core';
import {
  AiReviewRunResult,
  AiReviewsService,
} from '@core/services/ai-reviews.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { refreshScopesForEntityTypes } from '@core/util/entity-refresh-scopes';

export interface TaskReviewRun {
  startedAt: number;
  progress: number;
}

@Service()
export class TaskReviewRunsService {
  private readonly reviews = inject(AiReviewsService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly runs = signal<ReadonlyMap<number, TaskReviewRun>>(new Map());

  forTask(taskId: Signal<number | undefined>) {
    return computed(() => {
      const id = taskId();

      return id === undefined ? null : (this.runs().get(id) ?? null);
    });
  }

  async run(taskId: number): Promise<AiReviewRunResult | null> {
    const isAlreadyRunning = this.runs().has(taskId);

    if (isAlreadyRunning) return null;

    this.set(taskId, { startedAt: Date.now(), progress: 0 });

    const result = await this.reviews.runTaskReview(taskId, (characters) => {
      const current = this.runs().get(taskId);

      if (!current) return;

      this.set(taskId, { ...current, progress: characters });
    });

    this.runs.update((runs) => {
      const remaining = new Map(runs);

      remaining.delete(taskId);

      return remaining;
    });

    this.workspaceRefresh.refresh(refreshScopesForEntityTypes(['aiReview']));

    return result;
  }

  private set(taskId: number, run: TaskReviewRun) {
    this.runs.update((runs) => new Map(runs).set(taskId, run));
  }
}
