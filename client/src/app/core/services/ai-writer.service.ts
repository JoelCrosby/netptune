import { Service, inject } from '@angular/core';
import { AiStreamEvent, AiStreamEventType } from '@core/models/ai-conversation';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { aiRequestHeaders } from '@core/util/ai-request-headers';
import { readEventStream } from '@core/util/event-stream';
import { environment } from '@env/environment';

export interface AiWriteRequest {
  prompt: string;
  before: string;
  after: string;
}

@Service()
export class AiWriterService {
  private readonly workspace = inject(WorkspaceService);
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;

  private readonly unreachable = $localize`:Shown in an editor when the inline assistant request fails before any reply:The assistant could not be reached. Try again.`;
  private readonly rateLimited = $localize`:Shown in an editor when inline assistant requests are being sent too quickly:Too many assistant requests. Try again in a minute.`;
  private readonly unfinished = $localize`:Shown in an editor when the inline assistant reply ends early:The assistant stopped before finishing.`;

  writeTaskDescription(
    systemId: string,
    request: AiWriteRequest,
    write: (text: string) => void,
    signal: AbortSignal
  ): Promise<void> {
    const path = `api/ai/tasks/${encodeURIComponent(systemId)}/description`;

    return this.stream(path, request, write, signal);
  }

  writePage(
    pageId: number,
    request: AiWriteRequest,
    write: (text: string) => void,
    signal: AbortSignal
  ): Promise<void> {
    return this.stream(`api/ai/pages/${pageId}/write`, request, write, signal);
  }

  private async stream(
    path: string,
    request: AiWriteRequest,
    write: (text: string) => void,
    signal: AbortSignal
  ): Promise<void> {
    const url = `${environment.apiEndpoint}${path}`;
    const response = await fetch(url, {
      method: 'POST',
      credentials: 'include',
      headers: this.createHeaders(),
      signal,
      body: JSON.stringify(request),
    }).catch(() => null);

    if (signal.aborted) return;

    if (!response?.ok || !response.body) {
      const isRateLimited = response?.status === 429;

      throw new Error(isRateLimited ? this.rateLimited : this.unreachable);
    }

    let failure: string | null = null;
    let completed = false;

    const onEvent = (event: AiStreamEvent) => {
      switch (event.type) {
        case AiStreamEventType.textDelta:
          write(event.text ?? '');
          break;
        case AiStreamEventType.turnCompleted:
          completed = true;
          break;
        case AiStreamEventType.error:
          failure = event.message ?? this.unreachable;
          break;
      }
    };

    await readEventStream(response.body, onEvent).catch(() => null);

    if (signal.aborted) return;

    if (failure) {
      throw new Error(failure);
    }

    if (!completed) {
      throw new Error(this.unfinished);
    }
  }

  private createHeaders(): Record<string, string> {
    const headers = aiRequestHeaders(
      this.workspace.getWorkspaceRoute(),
      this.workspaceSlug()
    );

    return { ...headers, Accept: 'text/event-stream' };
  }
}
