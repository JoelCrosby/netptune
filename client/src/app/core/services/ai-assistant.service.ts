import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
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

interface AiStoredSession {
  workspace: string;
  conversationId: string | null;
  isOpen: boolean;
}

const STREAM_PREFIX = 'data: ';
const MODE_STORAGE_KEY = 'ai-assistant.mode';
const SESSION_STORAGE_KEY = 'ai-assistant.session';
const DRAFT_STORAGE_KEY = 'ai-assistant.drafts';
const DRAFT_PERSIST_DELAY = 400;
const NEW_CONVERSATION_KEY = 'new';
const ASSISTANT_PAGE_PATTERN = /^\/[^/]+\/assistant$/;

/** Matches the server's turn timeout — a reply cannot arrive after it. */
const RESUME_TIMEOUT = 5 * 60 * 1000;
const RESUME_POLL_INTERVAL = 2000;

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
  readonly isThinking = signal(false);
  readonly conversationId = signal<string | null>(null);
  readonly conversationTitle = signal<string | null>(null);
  readonly changeSet = signal<AiChangeSet | null>(null);
  readonly excludedChangeIds = signal<Set<number>>(new Set());
  readonly isApplying = signal(false);
  readonly conversations = signal<AiConversation[]>([]);
  readonly showHistory = signal(false);
  readonly models = signal<AiModelOption[]>([]);
  readonly references = signal<Map<string, AiEntityReference>>(new Map());
  readonly transcriptVersion = signal(0);
  readonly hasCredentials = signal<boolean | null>(null);
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

  readonly draft = signal('');

  private hasRestoredSession = false;
  private pendingSince: number | null = null;
  private drafts: Record<string, string> =
    this.storage.getItem<Record<string, string>>(DRAFT_STORAGE_KEY) ?? {};

  private draftTimer: ReturnType<typeof setTimeout> | null = null;

  /** Drafts follow the chat they were typed in, so switching chats swaps them. */
  private readonly draftKey = computed(() => {
    const workspace = this.workspaceKey() ?? '';
    const conversationId = this.conversationId() ?? NEW_CONVERSATION_KEY;

    return `${workspace}:${conversationId}`;
  });

  constructor() {
    effect(() => {
      const key = this.draftKey();

      this.draft.set(this.drafts[key] ?? '');
    });

    effect(() => {
      const workspace = this.workspaceKey();
      const isAvailable = this.isAvailable();
      const canRestore =
        isAvailable && workspace !== null && !this.hasRestoredSession;

      if (!canRestore) {
        return;
      }

      this.hasRestoredSession = true;

      void this.restoreSession(workspace);
    });

    effect(() => {
      const session: AiStoredSession = {
        workspace: this.workspaceKey() ?? '',
        conversationId: this.conversationId(),
        isOpen: this.isOpen(),
      };

      const canRemember = session.workspace !== '' && this.hasRestoredSession;

      if (!canRemember) {
        return;
      }

      this.storage.setItem(SESSION_STORAGE_KEY, session);
    });
  }

  selectModel(modelId: string | null) {
    this.selectedModel.set(modelId);
  }

  setDraft(text: string) {
    const key = this.draftKey();

    this.draft.set(text);
    this.rememberDraft(key, text);
  }

  private rememberDraft(key: string, text: string) {
    this.drafts = this.withDraft(key, text);

    if (this.draftTimer !== null) {
      clearTimeout(this.draftTimer);
    }

    this.draftTimer = setTimeout(() => {
      this.draftTimer = null;
      this.storage.setItem(DRAFT_STORAGE_KEY, this.drafts);
    }, DRAFT_PERSIST_DELAY);
  }

  private withDraft(key: string, text: string): Record<string, string> {
    const hasText = text.trim().length > 0;

    if (hasText) {
      return { ...this.drafts, [key]: text };
    }

    const remaining = Object.entries(this.drafts).filter(([stored]) => {
      return stored !== key;
    });

    return Object.fromEntries(remaining);
  }

  private forgetDraft(key: string) {
    this.draft.set('');
    this.rememberDraft(key, '');
  }

  private async restoreSession(workspace: string) {
    const session = this.storage.getItem<AiStoredSession>(SESSION_STORAGE_KEY);
    const isSameWorkspace = session?.workspace === workspace;

    if (!session || !isSameWorkspace) {
      return;
    }

    const shouldOpen = session.isOpen && this.mode() !== 'dedicated';

    if (shouldOpen) {
      this.isOpen.set(true);
    }

    if (!session.conversationId) {
      return;
    }

    await this.loadModels();

    const detail = await this.readConversation(session.conversationId);

    if (!detail) {
      return;
    }

    this.applyConversation(detail);

    await this.resumeTurn();
  }

  async ensureLoaded() {
    await this.loadModels();
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

    this.hasCredentials.set(providers.size > 0);
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
    const detail = await this.readConversation(conversationId);

    if (!detail) {
      return;
    }

    this.applyConversation(detail);
  }

  private async readConversation(
    conversationId: string
  ): Promise<AiConversationDetail | null> {
    try {
      const response = await this.http
        .get<ClientResponse<AiConversationDetail>>(
          `api/ai/conversations/${conversationId}`
        )
        .toPromise();

      return response?.payload ?? null;
    } catch {
      return null;
    }
  }

  private applyConversation(detail: AiConversationDetail) {
    const messages = detail.messages;
    const last = messages[messages.length - 1];
    const isAwaitingReply = last?.role === AiMessageRole.user;

    this.conversationId.set(detail.conversation.id);
    this.conversationTitle.set(detail.conversation.title);
    this.selectedModel.set(detail.conversation.model);
    this.entries.set(messages.map((message) => this.toEntry(message)));
    this.addReferences(messages.flatMap((message) => message.references));
    this.changeSet.set(detail.pendingChangeSet ?? null);
    this.excludedChangeIds.set(new Set());
    this.showHistory.set(false);
    this.pendingSince = isAwaitingReply ? Date.parse(last.createdAt) : null;
  }

  /**
   * A reload drops the event stream, but the server finishes and stores the turn
   * regardless, so wait for the reply to land instead of losing it.
   */
  private async resumeTurn() {
    const conversationId = this.conversationId();
    const startedAt = this.pendingSince;

    if (conversationId === null || startedAt === null) {
      return;
    }

    const isExpired = Date.now() - startedAt >= RESUME_TIMEOUT;

    if (isExpired) {
      return;
    }

    this.appendEntry({ role: 'assistant', text: '', tools: [] });
    this.isStreaming.set(true);
    this.isThinking.set(true);

    try {
      await this.awaitReply(conversationId, startedAt);
    } finally {
      this.isStreaming.set(false);
      this.isThinking.set(false);
      this.pendingSince = null;
    }
  }

  private async awaitReply(conversationId: string, startedAt: number) {
    for (;;) {
      await this.wait(RESUME_POLL_INTERVAL);

      const isCurrent = this.conversationId() === conversationId;

      if (!isCurrent) {
        return;
      }

      const detail = await this.readReply(conversationId);

      if (detail) {
        this.applyConversation(detail);

        return;
      }

      const isExpired = Date.now() - startedAt >= RESUME_TIMEOUT;

      if (isExpired) {
        this.failLastEntry(
          $localize`:Shown when the assistant reports a failure:The assistant stopped unexpectedly.`
        );

        return;
      }
    }
  }

  private async readReply(
    conversationId: string
  ): Promise<AiConversationDetail | null> {
    const detail = await this.readConversation(conversationId);
    const messages = detail?.messages ?? [];
    const last = messages[messages.length - 1];
    const hasReply = last !== undefined && last.role !== AiMessageRole.user;

    return hasReply ? detail : null;
  }

  private wait(duration: number) {
    return new Promise<void>((resolve) => setTimeout(resolve, duration));
  }

  async deleteConversation(conversationId: string) {
    await this.http
      .delete(`api/ai/conversations/${conversationId}`)
      .toPromise();

    this.rememberDraft(`${this.workspaceKey() ?? ''}:${conversationId}`, '');

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
    this.transcriptVersion.update((version) => version + 1);
    this.pendingSince = null;
    this.forgetDraft(`${this.workspaceKey() ?? ''}:${NEW_CONVERSATION_KEY}`);
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

    this.forgetDraft(this.draftKey());
    this.appendEntry({ role: 'user', text: trimmed, tools: [] });
    this.appendEntry({ role: 'assistant', text: '', tools: [] });
    this.isStreaming.set(true);
    this.isThinking.set(true);

    try {
      await this.stream(trimmed);
    } catch {
      this.failLastEntry(
        $localize`:Shown when the assistant request fails:The assistant could not be reached.`
      );
    } finally {
      this.isStreaming.set(false);
      this.isThinking.set(false);
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
      this.isThinking.set(false);
      this.appendText(event.text);

      return;
    }

    if (event.type === AiStreamEventType.toolStarted && event.toolName) {
      this.isThinking.set(true);
      this.appendTool(event.toolName);

      return;
    }

    if (event.type === AiStreamEventType.toolCompleted) {
      this.isThinking.set(true);

      return;
    }

    if (event.type === AiStreamEventType.turnCompleted) {
      this.isThinking.set(false);

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
      this.isThinking.set(false);
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
