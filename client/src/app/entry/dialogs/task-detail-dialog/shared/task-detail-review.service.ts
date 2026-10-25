import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import {
  AiReviewDecision,
  AiReviewFinding,
  AiReviewFindingStatus,
  DecideAiReviewFindingRequest,
} from '@core/models/ai-review';
import { RefreshScope } from '@core/models/refresh-scope';
import { taskReviewResource } from '@core/resources/ai-review.resource';
import { AiReviewsService } from '@core/services/ai-reviews.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { refreshScopesForEntityTypes } from '@core/util/entity-refresh-scopes';
import { getErrorMessage } from '@core/util/error-message';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { firstValueFrom } from 'rxjs';
import { TaskDetailService } from '../task-detail.service';

@Injectable()
export class TaskDetailReviewService {
  private readonly taskDetail = inject(TaskDetailService);
  private readonly reviews = inject(AiReviewsService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly resource = taskReviewResource(this.taskDetail.taskId);

  readonly latest = computed(() => {
    return this.resource.hasValue() ? this.resource.value() : undefined;
  });

  readonly current = computed(() => this.latest()?.current ?? null);
  readonly findings = computed(() => this.current()?.findings ?? []);
  readonly openCount = computed(() => this.latest()?.openCount ?? 0);
  readonly loading = this.resource.isLoading;

  readonly hasReview = computed(() => {
    const latest = this.latest();

    return !!latest?.current || !!latest?.latestFailure;
  });

  readonly openFindings = computed(() => {
    return this.findings().filter(
      (finding) => finding.status === AiReviewFindingStatus.open
    );
  });

  readonly decidedFindings = computed(() => {
    return this.findings().filter(
      (finding) => finding.status !== AiReviewFindingStatus.open
    );
  });

  readonly running = signal(false);
  readonly progress = signal(0);
  readonly runError = signal<string | null>(null);
  readonly decidingFindingId = signal<number | null>(null);

  async run() {
    const taskId = this.taskDetail.taskId();
    const isAlreadyRunning = this.running();

    if (!taskId || isAlreadyRunning) return;

    this.running.set(true);
    this.progress.set(0);
    this.runError.set(null);

    const result = await this.reviews.runTaskReview(taskId, (characters) => {
      this.progress.set(characters);
    });

    this.running.set(false);
    this.runError.set(result.failure);
    this.resource.reload();
  }

  dismiss(finding: AiReviewFinding, reason: string) {
    const trimmed = reason.trim();

    return this.decide(finding, {
      decision: AiReviewDecision.dismiss,
      reason: trimmed || null,
    });
  }

  reopen(finding: AiReviewFinding, revert = false) {
    return this.decide(finding, { decision: AiReviewDecision.reopen, revert });
  }

  accept(
    finding: AiReviewFinding,
    choices: Omit<DecideAiReviewFindingRequest, 'decision'>
  ) {
    return this.decide(finding, {
      ...choices,
      decision: AiReviewDecision.accept,
    });
  }

  private async decide(
    finding: AiReviewFinding,
    request: DecideAiReviewFindingRequest
  ): Promise<boolean> {
    const runId = this.current()?.run.id;

    if (!runId) return false;

    this.decidingFindingId.set(finding.id);

    const decision = this.reviews
      .decide(runId, finding.id, request)
      .pipe(unwrapClientResponse());

    try {
      await firstValueFrom(decision);

      const changesTheTask =
        request.decision === AiReviewDecision.accept || !!request.revert;

      this.workspaceRefresh.refresh(decisionRefreshScopes(changesTheTask));

      return true;
    } catch (error) {
      this.snackbar.error(decisionErrorMessage(error));
      this.resource.reload();

      return false;
    } finally {
      this.decidingFindingId.set(null);
    }
  }
}

// The server broadcasts the same scopes, but never back to the tab that made the change.
function decisionRefreshScopes(changesTheTask: boolean): Set<RefreshScope> {
  const entityTypes = changesTheTask
    ? ['aiReview', 'task', 'tag']
    : ['aiReview'];

  return refreshScopesForEntityTypes(entityTypes);
}

function decisionErrorMessage(error: unknown): string {
  const isForbidden =
    error instanceof HttpErrorResponse && error.status === 403;

  if (isForbidden) {
    return $localize`:Shown when someone without permission accepts, dismisses or reopens a review finding:You don't have permission to make this change.`;
  }

  return getErrorMessage(error);
}
