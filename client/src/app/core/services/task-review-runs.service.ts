import { Service, Signal, computed, inject, signal } from '@angular/core';
import {
  AiReviewRunResult,
  AiReviewsService,
} from '@core/services/ai-reviews.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import {
  AiReviewLiveRun,
  applyAiReviewEvent,
  startAiReviewLiveRun,
} from '@core/util/ai-review-live-run';
import { refreshScopesForEntityTypes } from '@core/util/entity-refresh-scopes';

@Service()
export class TaskReviewRunsService {
  private readonly reviews = inject(AiReviewsService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly runs = signal<ReadonlyMap<number, AiReviewLiveRun>>(
    new Map()
  );

  forTask(taskId: Signal<number | undefined>) {
    return computed(() => {
      const id = taskId();

      return id === undefined ? null : (this.runs().get(id) ?? null);
    });
  }

  async run(taskId: number): Promise<AiReviewRunResult | null> {
    const isAlreadyRunning = this.runs().has(taskId);

    if (isAlreadyRunning) return null;

    this.set(taskId, startAiReviewLiveRun());

    const result = await this.reviews.runTaskReview(taskId, (event) => {
      const current = this.runs().get(taskId);

      if (!current) return;

      this.set(taskId, applyAiReviewEvent(current, event));
    });

    this.runs.update((runs) => {
      const remaining = new Map(runs);

      remaining.delete(taskId);

      return remaining;
    });

    this.workspaceRefresh.refresh(refreshScopesForEntityTypes(['aiReview']));

    return result;
  }

  private set(taskId: number, run: AiReviewLiveRun) {
    this.runs.update((runs) => new Map(runs).set(taskId, run));
  }
}
