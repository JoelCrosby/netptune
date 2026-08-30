import { Service, LOCALE_ID, inject } from '@angular/core';
import { AiApplyProgress } from '@core/models/ai-apply-progress';
import { AiQuestionAnswer, AiStreamEvent } from '@core/models/ai-conversation';
import { AiEffort } from '@core/models/ai-effort';
import { AiContextService } from '@core/services/ai-context.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { readEventStream } from '@core/util/event-stream';
import { environment } from '@env/environment';

export interface AiReviseTarget {
  changeSetId: string;
  changeId: number;
}

export interface AiTurnRequest {
  conversationId: string | null;
  text: string;
  model: string | null;
  effort: AiEffort | null;
  retry: boolean;
  answer: AiQuestionAnswer | null;
  revise: AiReviseTarget | null;
}

export interface AiApplyRequest {
  changeSetId: string;
  changeIds: number[];
}

@Service()
export class AiStreamService {
  private readonly workspace = inject(WorkspaceService);
  private readonly workspaceId = inject(CurrentWorkspaceService).slug;
  private readonly context = inject(AiContextService);
  private readonly locale = inject(LOCALE_ID);

  private abort: AbortController | null = null;

  async run(
    request: AiTurnRequest,
    onEvent: (event: AiStreamEvent) => void
  ): Promise<boolean> {
    const abort = new AbortController();

    this.abort = abort;

    try {
      const response = await fetch(
        `${environment.apiEndpoint}api/ai/conversations/messages`,
        {
          method: 'POST',
          credentials: 'include',
          headers: this.createHeaders(),
          signal: abort.signal,
          body: JSON.stringify(this.createBody(request)),
        }
      );

      if (!response.ok || !response.body) {
        return false;
      }

      await readEventStream(response.body, onEvent);

      return true;
    } finally {
      const isCurrent = this.abort === abort;

      if (isCurrent) {
        this.abort = null;
      }
    }
  }

  cancel() {
    this.abort?.abort();
    this.abort = null;
  }

  /**
   * A change set is applied one change at a time, so the request reports each one as it lands
   * rather than answering once at the end.
   */
  async runApply(
    request: AiApplyRequest,
    onEvent: (progress: AiApplyProgress) => void
  ): Promise<boolean> {
    const response = await fetch(
      `${environment.apiEndpoint}api/ai/change-sets/${request.changeSetId}/apply`,
      {
        method: 'POST',
        credentials: 'include',
        headers: {
          ...this.createHeaders(),
          Accept: 'text/event-stream',
        },
        body: JSON.stringify({ changeIds: request.changeIds }),
      }
    );

    if (!response.ok || !response.body) {
      return false;
    }

    await readEventStream(response.body, onEvent);

    return true;
  }

  private createBody(request: AiTurnRequest) {
    return {
      conversationId: request.conversationId,
      text: request.text,
      model: request.model,
      effort: request.effort,
      locale: this.locale,
      retry: request.retry,
      answer: request.answer,
      revise: request.revise,
      context: this.context.context(),
    };
  }

  private createHeaders(): Record<string, string> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    const workspaceRoute = this.workspace.getWorkspaceRoute();
    const workspaceHeader = workspaceRoute ?? this.workspaceId();

    if (workspaceHeader) {
      headers['workspace'] = workspaceHeader;
    }

    return headers;
  }
}
