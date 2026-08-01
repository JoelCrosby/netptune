import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  AiChangeSet,
  AiConversation,
  AiConversationDetail,
  AiMessageRole,
  AiStreamEvent,
  AiStreamEventType,
} from '@core/models/ai-conversation';
import { WorkspaceService } from '@core/services/workspace.service';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';

export interface AiChatEntry {
  role: 'user' | 'assistant';
  text: string;
  tools: string[];
  failed?: boolean;
}

const STREAM_PREFIX = 'data: ';

@Injectable({ providedIn: 'root' })
export class AiAssistantService {
  private readonly store = inject(Store);
  private readonly workspace = inject(WorkspaceService);
  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  private readonly http = inject(HttpClient);

  readonly isOpen = signal(false);
  readonly entries = signal<AiChatEntry[]>([]);
  readonly isStreaming = signal(false);
  readonly conversationId = signal<string | null>(null);
  readonly changeSet = signal<AiChangeSet | null>(null);
  readonly excludedChangeIds = signal<Set<number>>(new Set());
  readonly isApplying = signal(false);
  readonly conversations = signal<AiConversation[]>([]);
  readonly showHistory = signal(false);

  async toggleHistory() {
    const next = !this.showHistory();

    this.showHistory.set(next);

    if (next) {
      await this.loadConversations();
    }
  }

  async loadConversations() {
    const conversations = await this.http
      .get<AiConversation[]>('api/ai/conversations')
      .toPromise();

    this.conversations.set(conversations ?? []);
  }

  async openConversation(conversationId: string) {
    const response = await this.http
      .get<ClientResponse<AiConversationDetail>>(
        `api/ai/conversations/${conversationId}`
      )
      .toPromise();

    const detail = response?.payload;

    if (!detail) {
      return;
    }

    this.conversationId.set(detail.conversation.id);
    this.entries.set(detail.messages.map((message) => this.toEntry(message)));
    this.changeSet.set(null);
    this.excludedChangeIds.set(new Set());
    this.showHistory.set(false);
  }

  async deleteConversation(conversationId: string) {
    await this.http
      .delete(`api/ai/conversations/${conversationId}`)
      .toPromise();

    const isCurrent = this.conversationId() === conversationId;

    if (isCurrent) {
      this.startNewConversation();
    }

    await this.loadConversations();
  }

  private toEntry(message: {
    role: AiMessageRole;
    text?: string;
    toolNames: string[];
  }): AiChatEntry {
    return {
      role: message.role === AiMessageRole.user ? 'user' : 'assistant',
      text: message.text ?? '',
      tools: message.toolNames,
    };
  }

  open() {
    this.isOpen.set(true);
  }

  close() {
    this.isOpen.set(false);
  }

  toggle() {
    this.isOpen.update((value) => !value);
  }

  startNewConversation() {
    this.conversationId.set(null);
    this.entries.set([]);
    this.changeSet.set(null);
    this.excludedChangeIds.set(new Set());
    this.showHistory.set(false);
  }

  toggleChange(changeId: number) {
    this.excludedChangeIds.update((current) => {
      const next = new Set(current);
      const wasExcluded = next.has(changeId);

      if (wasExcluded) {
        next.delete(changeId);
      } else {
        next.add(changeId);
      }

      return next;
    });
  }

  async applyChangeSet() {
    const changeSet = this.changeSet();

    if (!changeSet || this.isApplying()) {
      return;
    }

    const excluded = this.excludedChangeIds();
    const changeIds = changeSet.changes
      .filter((change) => !excluded.has(change.id))
      .map((change) => change.id);

    if (changeIds.length === 0) {
      return;
    }

    this.isApplying.set(true);

    try {
      await this.http
        .post(`api/ai/change-sets/${changeSet.id}/apply`, { changeIds })
        .toPromise();

      await this.refreshChangeSet(changeSet.id);
    } finally {
      this.isApplying.set(false);
    }
  }

  async discardChangeSet() {
    const changeSet = this.changeSet();

    if (!changeSet) {
      return;
    }

    await this.http
      .post(`api/ai/change-sets/${changeSet.id}/discard`, {})
      .toPromise();

    this.changeSet.set(null);
  }

  private async refreshChangeSet(changeSetId: string) {
    const response = await this.http
      .get<ClientResponse<AiChangeSet>>(`api/ai/change-sets/${changeSetId}`)
      .toPromise();

    this.changeSet.set(response?.payload ?? null);
  }

  async send(text: string) {
    const trimmed = text.trim();

    if (!trimmed || this.isStreaming()) {
      return;
    }

    this.appendEntry({ role: 'user', text: trimmed, tools: [] });
    this.appendEntry({ role: 'assistant', text: '', tools: [] });
    this.isStreaming.set(true);

    try {
      await this.stream(trimmed);
    } catch {
      this.failLastEntry(
        $localize`:Shown when the assistant request fails:The assistant could not be reached.`
      );
    } finally {
      this.isStreaming.set(false);
    }
  }

  private async stream(text: string) {
    const response = await fetch(
      `${environment.apiEndpoint}api/ai/conversations/messages`,
      {
        method: 'POST',
        credentials: 'include',
        headers: this.createHeaders(),
        body: JSON.stringify({
          conversationId: this.conversationId(),
          text,
        }),
      }
    );

    if (!response.ok || !response.body) {
      this.failLastEntry(
        $localize`:Shown when the assistant request fails:The assistant could not be reached.`
      );

      return;
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    for (;;) {
      const { done, value } = await reader.read();

      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });

      const chunks = buffer.split('\n\n');
      buffer = chunks.pop() ?? '';

      for (const chunk of chunks) {
        this.handleChunk(chunk);
      }
    }
  }

  private handleChunk(chunk: string) {
    const line = chunk.trim();

    if (!line.startsWith(STREAM_PREFIX)) {
      return;
    }

    const payload = line.slice(STREAM_PREFIX.length);
    const event = this.parseEvent(payload);

    if (!event) {
      return;
    }

    this.applyEvent(event);
  }

  private parseEvent(payload: string): AiStreamEvent | null {
    try {
      return JSON.parse(payload) as AiStreamEvent;
    } catch {
      return null;
    }
  }

  private applyEvent(event: AiStreamEvent) {
    if (
      event.type === AiStreamEventType.conversationStarted &&
      event.conversationId
    ) {
      this.conversationId.set(event.conversationId);

      return;
    }

    if (event.type === AiStreamEventType.textDelta && event.text) {
      this.appendText(event.text);

      return;
    }

    if (event.type === AiStreamEventType.toolStarted && event.toolName) {
      this.appendTool(event.toolName);

      return;
    }

    if (
      event.type === AiStreamEventType.changeSetProposed &&
      event.changeSetId
    ) {
      void this.refreshChangeSet(event.changeSetId);

      return;
    }

    if (event.type === AiStreamEventType.error) {
      this.failLastEntry(
        event.message ??
          $localize`:Shown when the assistant reports a failure:The assistant stopped unexpectedly.`
      );
    }
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

  private appendEntry(entry: AiChatEntry) {
    this.entries.update((current) => [...current, entry]);
  }

  private appendText(text: string) {
    this.entries.update((current) => {
      const next = [...current];
      const last = next[next.length - 1];

      next[next.length - 1] = { ...last, text: last.text + text };

      return next;
    });
  }

  private appendTool(toolName: string) {
    this.entries.update((current) => {
      const next = [...current];
      const last = next[next.length - 1];

      next[next.length - 1] = { ...last, tools: [...last.tools, toolName] };

      return next;
    });
  }

  private failLastEntry(message: string) {
    this.entries.update((current) => {
      const next = [...current];
      const last = next[next.length - 1];

      next[next.length - 1] = { ...last, text: message, failed: true };

      return next;
    });
  }
}
