import { Service, LOCALE_ID, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AiStreamEvent } from '@core/models/ai-conversation';
import { CurrentProjectService } from '@core/services/current-project.service';
import { CurrentTaskService } from '@core/services/current-task.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { buildClientContext } from '@core/util/ai-client-context';
import { environment } from '@env/environment';

export interface AiTurnRequest {
  conversationId: string | null;
  text: string;
  model: string | null;
  retry: boolean;
}

const STREAM_PREFIX = 'data: ';

@Service()
export class AiStreamService {
  private readonly router = inject(Router);
  private readonly workspace = inject(WorkspaceService);
  private readonly workspaceId = inject(CurrentWorkspaceService).slug;
  private readonly currentProject = inject(CurrentProjectService).current;
  private readonly selectedTask = inject(CurrentTaskService).task;
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

      await this.read(response.body, onEvent);

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

  private async read(
    body: ReadableStream<Uint8Array>,
    onEvent: (event: AiStreamEvent) => void
  ) {
    const reader = body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    for (;;) {
      const { done, value } = await reader.read();

      if (done) {
        return;
      }

      buffer += decoder.decode(value, { stream: true });

      const chunks = buffer.split('\n\n');
      buffer = chunks.pop() ?? '';

      for (const chunk of chunks) {
        const event = this.parseChunk(chunk);

        if (event) {
          onEvent(event);
        }
      }
    }
  }

  private parseChunk(chunk: string): AiStreamEvent | null {
    const line = chunk.trim();

    if (!line.startsWith(STREAM_PREFIX)) {
      return null;
    }

    try {
      return JSON.parse(line.slice(STREAM_PREFIX.length)) as AiStreamEvent;
    } catch {
      return null;
    }
  }

  private createBody(request: AiTurnRequest) {
    return {
      conversationId: request.conversationId,
      text: request.text,
      model: request.model,
      locale: this.locale,
      retry: request.retry,
      context: buildClientContext({
        url: this.router.url,
        project: this.currentProject(),
        task: this.selectedTask(),
      }),
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
