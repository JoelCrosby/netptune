import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  AiReviewStreamEvent,
  AiReviewStreamEventType,
  DecideAiReviewFindingRequest,
} from '@core/models/ai-review';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { aiRequestHeaders } from '@core/util/ai-request-headers';
import { readEventStream } from '@core/util/event-stream';
import { environment } from '@env/environment';

export interface AiReviewRunResult {
  runId: number | null;
  failure: string | null;
}

export interface AiReviewRunListener {
  progress: (characters: number) => void;
  tool: (label: string) => void;
}

@Service()
export class AiReviewsService {
  private readonly http = inject(HttpClient);
  private readonly workspace = inject(WorkspaceService);
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;

  private readonly unreachable = $localize`:Shown on the Review tab when the review request fails before any reply:The assistant could not be reached. Try again.`;
  private readonly rateLimited = $localize`:Shown on the Review tab when reviews are being requested too quickly:Too many assistant requests. Try again in a minute.`;
  private readonly unfinished = $localize`:Shown on the Review tab when the review stream ends early:The review stopped before finishing. Reload to see whether it was saved.`;

  async runTaskReview(
    taskId: number,
    listener: AiReviewRunListener
  ): Promise<AiReviewRunResult> {
    const url = `${environment.apiEndpoint}api/tasks/${taskId}/reviews`;
    const response = await fetch(url, {
      method: 'POST',
      credentials: 'include',
      headers: this.createHeaders(),
    }).catch(() => null);

    if (!response?.ok || !response.body) {
      const isRateLimited = response?.status === 429;

      return {
        runId: null,
        failure: isRateLimited ? this.rateLimited : this.unreachable,
      };
    }

    let result: AiReviewRunResult = { runId: null, failure: this.unfinished };

    const onEvent = (event: AiReviewStreamEvent) => {
      switch (event.type) {
        case AiReviewStreamEventType.progress:
          listener.progress(event.characters ?? 0);
          break;
        case AiReviewStreamEventType.tool:
          listener.tool(event.tool ?? '');
          break;
        case AiReviewStreamEventType.completed:
          result = { runId: event.runId ?? null, failure: null };
          break;
        case AiReviewStreamEventType.failed:
          result = {
            runId: event.runId ?? null,
            failure: event.message ?? this.unreachable,
          };
          break;
      }
    };

    await readEventStream(response.body, onEvent).catch(() => null);

    return result;
  }

  decide(
    runId: number,
    findingId: number,
    request: DecideAiReviewFindingRequest
  ) {
    return this.http.post<ClientResponse>(
      `api/ai/reviews/${runId}/findings/${findingId}/decide`,
      request
    );
  }

  private createHeaders(): Record<string, string> {
    const headers = aiRequestHeaders(
      this.workspace.getWorkspaceRoute(),
      this.workspaceSlug()
    );

    return { ...headers, Accept: 'text/event-stream' };
  }
}
