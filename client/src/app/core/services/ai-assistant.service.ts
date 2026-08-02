import { HttpClient } from '@angular/common/http';
import {
  Injectable,
  LOCALE_ID,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { AiCredentialAvailability } from '@core/models/ai-credential';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { AiModelOption } from '@core/models/ai-model';
import { ClientResponse } from '@core/models/client-response';
import {
  AiChangeSet,
  AiChangeSetStatus,
  AiConversation,
  AiConversationDetail,
  AiEntityReference,
  AiMessageRole,
  AiStreamEvent,
  AiStreamEventType,
} from '@core/models/ai-conversation';
import { WorkspaceService } from '@core/services/workspace.service';
import { selectIsAssistantAvailable } from '@core/store/auth/auth.selectors';
import { selectCurrentProject } from '@core/store/projects/projects.selectors';
import { selectSelectedTask } from '@core/store/tasks/tasks.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { buildClientContext } from '@core/util/ai-client-context';
import { referenceKey } from '@core/util/ai-references';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';

export interface AiChatEntry {
  role: 'user' | 'assistant';
  text: string;
  tools: string[];
  failed?: boolean;
  stopped?: boolean;
}

/** Shared so a stored transcript renders the same wherever it is read back. */
export const toChatEntry = (message: {
  role: AiMessageRole;
  text?: string;
  toolNames: string[];
}): AiChatEntry => {
  return {
    role: message.role === AiMessageRole.user ? 'user' : 'assistant',
    text: message.text ?? '',
    tools: message.toolNames,
  };
};

interface AiWorkspaceSession {
  conversationId: string | null;
  isOpen: boolean;
  pendingTurnAt: number | null;
}

/** A chat belongs to the workspace it was started in, so sessions are kept per workspace. */
type AiStoredSessions = Record<string, AiWorkspaceSession>;

const STREAM_PREFIX = 'data: ';
const MODE_STORAGE_KEY = 'ai-assistant.mode';
const MODEL_STORAGE_KEY = 'ai-assistant.model';
const SESSION_STORAGE_KEY = 'ai-assistant.sessions';
const LEGACY_SESSION_STORAGE_KEY = 'ai-assistant.session';
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

  private readonly currentProject =
    this.store.selectSignal(selectCurrentProject);
  private readonly selectedTask = this.store.selectSignal(selectSelectedTask);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly storage = inject(LocalStorageService);
  private readonly locale = inject(LOCALE_ID);

  readonly isOpen = signal(false);
  readonly isAvailable = this.store.selectSignal(selectIsAssistantAvailable);
  readonly mode = signal<AiDisplayMode>(
    this.storage.getItem<AiDisplayMode>(MODE_STORAGE_KEY) ?? 'overlay'
  );

  readonly isVisible = computed(() => {
    return this.isOpen() && this.isAvailable();
  });

  readonly hasUnreadReply = signal(false);
  readonly isReplacingLastTurn = signal(false);

  private readonly transcriptViewers = signal(0);

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
  /** The model the user picked, where null means automatic — not the model a conversation resolved to. */
  readonly selectedModel = signal<string | null>(this.readModelPreference());

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

  private activeWorkspace: string | null = null;
  private isSwitchingWorkspace = false;
  private turnToken = 0;
  private streamAbort: AbortController | null = null;
  private isStopping = false;
  private pendingSince: number | null = null;

  private sessions: AiStoredSessions = this.readSessions();

  private readSessions(): AiStoredSessions {
    this.storage.removeItem(LEGACY_SESSION_STORAGE_KEY);

    return this.storage.getItem<AiStoredSessions>(SESSION_STORAGE_KEY) ?? {};
  }

  /**
   * Set while this browser has a turn in flight. A turn that failed clears it,
   * which is what separates a reply still being written from one that already
   * stopped — both leave the user message as the last thing on the server.
   */
  private readonly pendingTurnAt = signal<number | null>(null);

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
      const isReady = isAvailable && workspace !== null;

      if (!isReady || workspace === this.activeWorkspace) {
        return;
      }

      const isSwitch = this.activeWorkspace !== null;

      this.activeWorkspace = workspace;
      this.isSwitchingWorkspace = true;

      void this.enterWorkspace(workspace, isSwitch);
    });

    effect(() => {
      const session: AiWorkspaceSession = {
        conversationId: this.conversationId(),
        isOpen: this.isOpen(),
        pendingTurnAt: this.pendingTurnAt(),
      };

      const workspace = this.activeWorkspace;
      const canRemember = workspace !== null && !this.isSwitchingWorkspace;

      if (!canRemember) {
        return;
      }

      this.rememberSession(workspace, session);
    });
  }

  private rememberSession(workspace: string, session: AiWorkspaceSession) {
    this.sessions = { ...this.sessions, [workspace]: session };

    this.storage.setItem(SESSION_STORAGE_KEY, this.sessions);
  }

  selectModel(modelId: string | null) {
    this.selectedModel.set(modelId);
    this.storage.setItem(MODEL_STORAGE_KEY, modelId);
  }

  private readModelPreference(): string | null {
    return this.storage.getItem<string | null>(MODEL_STORAGE_KEY) ?? null;
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

  private async enterWorkspace(workspace: string, isSwitch: boolean) {
    const session = this.sessions[workspace] ?? null;

    if (isSwitch) {
      this.abandonTurn();
      this.clearConversation();
    }

    try {
      await this.restoreSession(session, isSwitch);
    } finally {
      this.isSwitchingWorkspace = false;

      this.rememberSession(workspace, {
        conversationId: this.conversationId(),
        isOpen: this.isOpen(),
        pendingTurnAt: this.pendingTurnAt(),
      });
    }
  }

  /** A chat from the workspace being left must not follow the user into the next one. */
  private clearConversation() {
    this.transcriptVersion.update((version) => version + 1);
    this.conversationId.set(null);
    this.conversationTitle.set(null);
    this.entries.set([]);
    this.references.set(new Map());
    this.changeSet.set(null);
    this.excludedChangeIds.set(new Set());
    this.conversations.set([]);
    this.showHistory.set(false);
    this.hasUnreadReply.set(false);
    this.isReplacingLastTurn.set(false);
    this.pendingSince = null;
  }

  private abandonTurn() {
    this.turnToken += 1;

    const hasTurn = this.isStreaming();

    if (!hasTurn) {
      return;
    }

    this.streamAbort?.abort();
    this.streamAbort = null;
    this.isStreaming.set(false);
    this.isThinking.set(false);
    this.pendingTurnAt.set(null);
  }

  private async restoreSession(
    session: AiWorkspaceSession | null,
    isSwitch: boolean
  ) {
    if (!session) {
      return;
    }

    const shouldOpen =
      !isSwitch && session.isOpen && this.mode() !== 'dedicated';

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

    const startedAt = session.pendingTurnAt;
    const wasTurnInFlight =
      startedAt !== null && Date.now() - startedAt < RESUME_TIMEOUT;

    if (!wasTurnInFlight) {
      this.pendingTurnAt.set(null);

      return;
    }

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

    const [catalog, availability] = await Promise.all([
      this.http.get<AiModelOption[]>('api/ai/models').toPromise(),
      this.http
        .get<AiCredentialAvailability>('api/ai/credentials/availability')
        .toPromise(),
    ]);

    const providers = new Set(
      (availability?.providers ?? []).map((item) => item.provider)
    );
    const available = (catalog ?? []).filter((model) => {
      return providers.has(model.provider);
    });

    this.hasCredentials.set(providers.size > 0);
    this.models.set(available);
    this.dropUnavailableModel(available);
  }

  /** A key can be removed after its model was picked, leaving a preference the server would reject. */
  private dropUnavailableModel(available: AiModelOption[]) {
    const selected = this.selectedModel();
    const isMissing =
      selected !== null && !available.some((model) => model.id === selected);

    if (!isMissing) {
      return;
    }

    this.selectModel(null);
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
    this.selectedModel.set(detail.conversation.requestedModel ?? null);
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
      this.pendingTurnAt.set(null);

      return;
    }

    this.appendEntry({ role: 'assistant', text: '', tools: [] });
    this.isStreaming.set(true);
    this.isThinking.set(true);

    const token = ++this.turnToken;

    try {
      await this.awaitReply(conversationId, startedAt, token);
    } finally {
      const isCurrent = this.turnToken === token;

      if (isCurrent) {
        this.isStreaming.set(false);
        this.isThinking.set(false);
        this.pendingTurnAt.set(null);
        this.pendingSince = null;
        this.markReplyReceived();
      }
    }
  }

  private async awaitReply(
    conversationId: string,
    startedAt: number,
    token: number
  ) {
    for (;;) {
      await this.wait(RESUME_POLL_INTERVAL);

      const isCurrent =
        this.turnToken === token && this.conversationId() === conversationId;

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
    return toChatEntry(message);
  }

  /**
   * The panel registers itself while it is on screen, so a reply that lands
   * behind a closed chat is the only one that raises the badge.
   */
  watchTranscript(): () => void {
    this.transcriptViewers.update((count) => count + 1);
    this.hasUnreadReply.set(false);

    return () => {
      this.transcriptViewers.update((count) => count - 1);
    };
  }

  private markReplyReceived() {
    const isWatched = this.transcriptViewers() > 0;

    if (isWatched) {
      return;
    }

    this.hasUnreadReply.set(true);
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
    this.selectedModel.set(this.readModelPreference());
    this.conversationId.set(null);
    this.conversationTitle.set(null);
    this.references.set(new Map());
    this.entries.set([]);
    this.changeSet.set(null);
    this.excludedChangeIds.set(new Set());
    this.showHistory.set(false);
  }

  toggleChanges(changeIds: number[]) {
    for (const changeId of changeIds) {
      this.toggleChange(changeId);
    }
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

  async undoChangeSet() {
    const changeSet = this.changeSet();

    if (!changeSet || this.isApplying()) {
      return;
    }

    this.isApplying.set(true);

    try {
      await this.http
        .post(`api/ai/change-sets/${changeSet.id}/undo`, {})
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

    await this.refreshChangeSet(changeSet.id);
  }

  stopTurn() {
    const isRunning = this.isStreaming();

    if (!isRunning || this.isStopping) {
      return;
    }

    this.isStopping = true;

    const conversationId = this.conversationId();

    if (conversationId) {
      void this.http
        .post(`api/ai/conversations/${conversationId}/stop`, {})
        .toPromise()
        .catch(() => undefined);
    }

    this.markLastEntryStopped();
    this.streamAbort?.abort();
  }

  async retryLastTurn() {
    await this.send('', true);
  }

  editLastQuestion() {
    const question = this.lastQuestion();

    if (question === null || this.isStreaming()) {
      return;
    }

    this.setDraft(question);
    this.isReplacingLastTurn.set(true);
  }

  cancelEdit() {
    this.isReplacingLastTurn.set(false);
  }

  private lastQuestion(): string | null {
    const entries = this.entries();

    for (let index = entries.length - 1; index >= 0; index -= 1) {
      const entry = entries[index];

      if (entry.role === 'user') {
        return entry.text;
      }
    }

    return null;
  }

  /** Drops the exchange being replaced, and answers with the question to ask again. */
  private rewindToLastQuestion(replacement: string): string | null {
    const entries = this.entries();
    let index = entries.length - 1;

    while (index >= 0 && entries[index].role !== 'user') {
      index -= 1;
    }

    if (index < 0) {
      return null;
    }

    const question = replacement.length > 0 ? replacement : entries[index].text;

    this.entries.set(entries.slice(0, index));
    this.changeSet.set(null);
    this.isReplacingLastTurn.set(false);

    return question;
  }

  private markLastEntryStopped() {
    this.entries.update((current) => {
      const next = [...current];
      const last = next[next.length - 1];

      if (!last || last.role !== 'assistant') {
        return current;
      }

      next[next.length - 1] = { ...last, stopped: true };

      return next;
    });
  }

  private async refreshChangeSet(changeSetId: string, token?: number) {
    try {
      const response = await this.http
        .get<ClientResponse<AiChangeSet>>(`api/ai/change-sets/${changeSetId}`)
        .toPromise();

      const payload = response?.payload ?? null;
      const isCurrent = token === undefined || this.turnToken === token;

      if (payload === null || !isCurrent) {
        return;
      }

      this.changeSet.set(payload);
    } catch {
      /* The turn recovers the proposals from the conversation instead. */
    }
  }

  async send(text: string, isRetry = false) {
    const trimmed = text.trim();
    const hasText = trimmed.length > 0;

    if ((!hasText && !isRetry) || this.isStreaming()) {
      return;
    }

    const wasNewConversation = this.conversationId() === null;
    const question = isRetry ? this.rewindToLastQuestion(trimmed) : trimmed;

    if (question === null) {
      return;
    }

    this.forgetDraft(this.draftKey());
    this.appendEntry({ role: 'user', text: question, tools: [] });
    this.appendEntry({ role: 'assistant', text: '', tools: [] });
    this.isStreaming.set(true);
    this.isThinking.set(true);
    this.pendingTurnAt.set(Date.now());

    const token = ++this.turnToken;

    this.isStopping = false;

    try {
      await this.stream(question, token, isRetry);
    } catch {
      const isCurrent = this.turnToken === token;

      if (isCurrent && !this.isStopping) {
        this.failLastEntry(
          $localize`:Shown when the assistant request fails:The assistant could not be reached.`
        );
      }
    } finally {
      const isCurrent = this.turnToken === token;

      if (isCurrent) {
        this.streamAbort = null;
        this.isStreaming.set(false);
        this.isThinking.set(false);
        this.pendingTurnAt.set(null);
        this.isStopping = false;
      }
    }

    const isAbandoned = this.turnToken !== token;

    if (isAbandoned) {
      return;
    }

    this.markReplyReceived();

    await this.recoverProposals(token);

    if (wasNewConversation) {
      await this.readGeneratedTitle();
    }
  }

  /**
   * A proposal reaches the client through a single event at the end of a turn,
   * so a dropped connection or a failed read leaves a stored change set with
   * nothing on screen pointing at it. Ask for it directly rather than lose it.
   */
  private async recoverProposals(token: number) {
    const isMissing = !this.hasPendingChangeSet();

    if (!isMissing) {
      return;
    }

    const conversationId = this.conversationId();

    if (conversationId === null) {
      return;
    }

    const pending = await this.readPendingChangeSet(conversationId);
    const isCurrent = this.turnToken === token && !this.hasPendingChangeSet();

    if (pending === null || !isCurrent) {
      return;
    }

    this.changeSet.set(pending);
    this.excludedChangeIds.set(new Set());
  }

  private hasPendingChangeSet(): boolean {
    return this.changeSet()?.status === AiChangeSetStatus.pending;
  }

  private async readPendingChangeSet(
    conversationId: string
  ): Promise<AiChangeSet | null> {
    try {
      const response = await this.http
        .get<ClientResponse<AiChangeSet>>(
          `api/ai/conversations/${conversationId}/change-set`
        )
        .toPromise();

      return response?.payload ?? null;
    } catch {
      return null;
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

  private async stream(text: string, token: number, isRetry = false) {
    const abort = new AbortController();

    this.streamAbort = abort;

    const response = await fetch(
      `${environment.apiEndpoint}api/ai/conversations/messages`,
      {
        method: 'POST',
        credentials: 'include',
        headers: this.createHeaders(),
        signal: abort.signal,
        body: JSON.stringify({
          conversationId: this.conversationId(),
          text,
          model: this.selectedModel(),
          locale: this.locale,
          retry: isRetry,
          context: buildClientContext({
            url: this.router.url,
            project: this.currentProject(),
            task: this.selectedTask(),
          }),
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
        this.handleChunk(chunk, token);
      }
    }
  }

  private handleChunk(chunk: string, token: number) {
    const isAbandoned = this.turnToken !== token;

    if (isAbandoned) {
      return;
    }

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

    if (event.type === AiStreamEventType.replyReset) {
      this.isThinking.set(true);
      this.resetLastEntry();

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
      void this.refreshChangeSet(event.changeSetId, this.turnToken);

      return;
    }

    if (event.type === AiStreamEventType.stopped) {
      this.isThinking.set(false);
      this.markLastEntryStopped();

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

  /** The harness discards a reply it caught claiming work it never did. */
  private resetLastEntry() {
    this.entries.update((current) => {
      const next = [...current];
      const last = next[next.length - 1];

      next[next.length - 1] = { ...last, text: '', tools: [] };

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
