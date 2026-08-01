import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AiCredential } from '@core/models/ai-credential';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { AiModelOption } from '@core/models/ai-model';
import { ClientResponse } from '@core/models/client-response';
import {
  AiChangeSet,
  AiConversation,
  AiConversationDetail,
  AiEntityReference,
  AiMessageRole,
  AiStreamEvent,
  AiStreamEventType,
} from '@core/models/ai-conversation';
import { WorkspaceService } from '@core/services/workspace.service';
import { selectIsAssistantAvailable } from '@core/store/auth/auth.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { referenceKey } from '@core/util/ai-references';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';

export interface AiChatEntry {
  role: 'user' | 'assistant';
  text: string;
  tools: string[];
  failed?: boolean;
}

const STREAM_PREFIX = 'data: ';
const MODE_STORAGE_KEY = 'ai-assistant.mode';
const ASSISTANT_PAGE_PATTERN = /^\/[^/]+\/assistant$/;

@Injectable({ providedIn: 'root' })
export class AiAssistantService {
  private readonly store = inject(Store);
  private readonly workspace = inject(WorkspaceService);
  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  readonly workspaceKey = computed(() => this.workspaceId() ?? null);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly storage = inject(LocalStorageService);

  readonly isOpen = signal(false);
  readonly isAvailable = this.store.selectSignal(selectIsAssistantAvailable);
  readonly mode = signal<AiDisplayMode>(
    this.storage.getItem<AiDisplayMode>(MODE_STORAGE_KEY) ?? 'overlay'
  );

  readonly isVisible = computed(() => {
    return this.isOpen() && this.isAvailable();
  });

  readonly isOverlayOpen = computed(() => {
    return this.isVisible() && this.mode() === 'overlay';
  });

  readonly isDocked = computed(() => {
    return this.isVisible() && this.mode() === 'docked';
  });

  readonly entries = signal<AiChatEntry[]>([]);
  readonly isStreaming = signal(false);
  readonly conversationId = signal<string | null>(null);
  readonly conversationTitle = signal<string | null>(null);
  readonly changeSet = signal<AiChangeSet | null>(null);
  readonly excludedChangeIds = signal<Set<number>>(new Set());
  readonly isApplying = signal(false);
  readonly conversations = signal<AiConversation[]>([]);
  readonly showHistory = signal(false);
  readonly models = signal<AiModelOption[]>([]);
  readonly references = signal<Map<string, AiEntityReference>>(new Map());
  readonly selectedModel = signal<string | null>(null);

  readonly selectedModelLabel = computed(() => {
    const selected = this.selectedModel();
    const model = this.models().find((option) => option.id === selected);

    if (model) {
      return model.label;
    }

    if (selected) {
      return selected;
    }

    return $localize`:Model option that lets the server choose:Automatic`;
  });

  selectModel(modelId: string | null) {
    this.selectedModel.set(modelId);
  }

  private async loadModels() {
    const hasModels = this.models().length > 0;

    if (hasModels) {
      return;
    }

    const [catalog, credentials] = await Promise.all([
      this.http.get<AiModelOption[]>('api/ai/models').toPromise(),
      this.http.get<AiCredential[]>('api/ai/credentials').toPromise(),
    ]);

    const providers = new Set((credentials ?? []).map((item) => item.provider));
    const available = (catalog ?? []).filter((model) => {
      return providers.has(model.provider);
    });

    this.models.set(available);
  }

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
    this.conversationTitle.set(detail.conversation.title);
    this.selectedModel.set(detail.conversation.model);
    this.entries.set(detail.messages.map((message) => this.toEntry(message)));
    this.addReferences(
      detail.messages.flatMap((message) => message.references)
    );
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
    if (!this.isAvailable()) {
      return;
    }

    const isDedicated = this.mode() === 'dedicated';

    if (isDedicated) {
      void this.openPage();

      return;
    }

    this.isOpen.set(true);

    void this.loadModels();
  }

  close() {
    this.isOpen.set(false);
  }

  toggle() {
    if (!this.isAvailable()) {
      return;
    }

    const isDedicated = this.mode() === 'dedicated';

    if (isDedicated) {
      void this.togglePage();

      return;
    }

    this.isOpen.update((value) => !value);

    if (this.isOpen()) {
      void this.loadModels();
    }
  }

  setMode(mode: AiDisplayMode) {
    this.mode.set(mode);
    this.storage.setItem(MODE_STORAGE_KEY, mode);

    if (mode === 'dedicated') {
      this.isOpen.set(false);

      void this.openPage();

      return;
    }

    if (this.isOnPage()) {
      void this.leavePage();
    }

    this.open();
  }

  private isOnPage(): boolean {
    const path = this.router.url.split('?')[0];

    return ASSISTANT_PAGE_PATTERN.test(path);
  }

  private workspaceRoute(): string | null {
    return this.workspace.getWorkspaceRoute() ?? this.workspaceId() ?? null;
  }

  private async openPage() {
    const workspace = this.workspaceRoute();

    if (!workspace) {
      return;
    }

    await this.loadModels();
    await this.router.navigate(['/', workspace, 'assistant']);
  }

  private async togglePage() {
    if (this.isOnPage()) {
      await this.leavePage();

      return;
    }

    await this.openPage();
  }

  private async leavePage() {
    const workspace = this.workspaceRoute();

    if (!workspace) {
      return;
    }

    await this.router.navigate(['/', workspace]);
  }

  addReferences(references: AiEntityReference[]) {
    if (references.length === 0) {
      return;
    }

    this.references.update((current) => {
      const next = new Map(current);

      for (const reference of references) {
        next.set(referenceKey(reference.type, reference.id), reference);
      }

      return next;
    });
  }

  startNewConversation() {
    this.conversationId.set(null);
    this.conversationTitle.set(null);
    this.references.set(new Map());
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

    const wasNewConversation = this.conversationId() === null;

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

    if (wasNewConversation) {
      await this.readGeneratedTitle();
    }
  }

  private async readGeneratedTitle() {
    const conversationId = this.conversationId();

    if (!conversationId) {
      return;
    }

    await this.loadConversations();

    const conversation = this.conversations().find((item) => {
      return item.id === conversationId;
    });

    this.conversationTitle.set(conversation?.title ?? null);
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
          model: this.selectedModel(),
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
      event.type === AiStreamEventType.entitiesReferenced &&
      event.references
    ) {
      this.addReferences(event.references);

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
