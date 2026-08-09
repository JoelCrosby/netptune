import { Injectable, computed, effect, inject, signal } from '@angular/core';
import {
  AiConversationDetail,
  AiMessageRole,
  AiStreamEvent,
  AiStreamEventType,
} from '@core/models/ai-conversation';
import { AiApiService } from '@core/services/ai-api.service';
import { AiChangeSetService } from '@core/services/ai-change-set.service';
import { AiConversationService } from '@core/services/ai-conversation.service';
import { AiDraftService } from '@core/services/ai-draft.service';
import { AiModelCatalogService } from '@core/services/ai-model-catalog.service';
import { AiPanelService } from '@core/services/ai-panel.service';
import {
  AiSessionService,
  AiWorkspaceSession,
} from '@core/services/ai-session.service';
import { AiStreamService } from '@core/services/ai-stream.service';
import { AiTranscriptService } from '@core/services/ai-transcript.service';
import { AiTurnProgressService } from '@core/services/ai-turn-progress.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';

/** Matches the server's turn timeout — a reply cannot arrive after it. */
const RESUME_TIMEOUT = 5 * 60 * 1000;
const RESUME_POLL_INTERVAL = 2000;

@Injectable({ providedIn: 'root' })
export class AiAssistantService {
  private readonly api = inject(AiApiService);
  private readonly panel = inject(AiPanelService);
  private readonly conversation = inject(AiConversationService);
  private readonly transcript = inject(AiTranscriptService);
  private readonly changeSets = inject(AiChangeSetService);
  private readonly catalog = inject(AiModelCatalogService);
  private readonly drafts = inject(AiDraftService);
  private readonly sessions = inject(AiSessionService);
  private readonly stream = inject(AiStreamService);
  private readonly progress = inject(AiTurnProgressService);
  private readonly workspaceId = inject(CurrentWorkspaceService).slug;

  readonly workspaceKey = computed(() => this.workspaceId() ?? null);
  readonly isAvailable = this.panel.isAvailable;

  readonly entries = this.transcript.entries;
  readonly references = this.transcript.references;
  readonly droppedMessages = this.transcript.droppedMessages;
  readonly usage = this.transcript.usage;
  readonly transcriptVersion = this.transcript.version;

  readonly conversationId = this.conversation.id;
  readonly conversationTitle = this.conversation.title;
  readonly conversations = this.conversation.conversations;
  readonly showHistory = this.conversation.showHistory;

  readonly changeSet = this.changeSets.changeSet;
  readonly excludedChangeIds = this.changeSets.excludedChangeIds;
  readonly isApplying = this.changeSets.isApplying;

  readonly models = this.catalog.models;
  readonly selectedModel = this.catalog.selectedModel;
  readonly selectedModelLabel = this.catalog.selectedModelLabel;
  readonly hasCredentials = this.catalog.hasCredentials;

  readonly draft = this.drafts.text;

  readonly turnUsage = this.progress.usage;
  readonly turnElapsedMs = this.progress.elapsedMs;

  readonly isStreaming = signal(false);
  readonly isThinking = signal(false);
  readonly isReplacingLastTurn = signal(false);

  /**
   * Set while this browser has a turn in flight. A turn that failed clears it,
   * which is what separates a reply still being written from one that already
   * stopped — both leave the user message as the last thing on the server.
   */
  private readonly pendingTurnAt = signal<number | null>(null);

  private activeWorkspace: string | null = null;
  private isSwitchingWorkspace = false;
  private turnToken = 0;
  private isStopping = false;

  constructor() {
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
      const session = this.currentSession();
      const workspace = this.activeWorkspace;
      const canRemember = workspace !== null && !this.isSwitchingWorkspace;

      if (!canRemember) {
        return;
      }

      this.sessions.remember(workspace, session);
    });
  }

  async ensureLoaded() {
    await this.catalog.load();
  }

  selectModel(modelId: string | null) {
    this.catalog.select(modelId);
  }

  setDraft(text: string) {
    this.drafts.set(text);
  }

  toggleChange(changeId: number) {
    this.changeSets.toggleChange(changeId);
  }

  toggleChanges(changeIds: number[]) {
    this.changeSets.toggleChanges(changeIds);
  }

  async applyChangeSet() {
    await this.changeSets.apply();
  }

  async retryFailedChanges() {
    await this.changeSets.retryFailed();
  }

  async undoChangeSet() {
    await this.changeSets.undo();
  }

  async discardChangeSet() {
    await this.changeSets.discard();
  }

  async toggleHistory() {
    await this.conversation.toggleHistory();
  }

  async loadConversations() {
    await this.conversation.loadList();
  }

  async openConversation(conversationId: string) {
    const detail = await this.conversation.read(conversationId);

    if (!detail) {
      return;
    }

    this.conversation.load(detail);
  }

  async deleteConversation(conversationId: string) {
    await this.conversation.remove(conversationId);

    this.drafts.discard(conversationId);

    const isCurrent = this.conversationId() === conversationId;

    if (isCurrent) {
      this.startNewConversation();
    }

    await this.conversation.loadList();
  }

  startNewConversation() {
    this.transcript.bumpVersion();
    this.drafts.clearNew();
    this.conversation.startNew();
    this.progress.reset();
  }

  /**
   * Opens the chat with the entity already named, so a question asked from a
   * task or sprint does not have to describe which one it is about.
   */
  askAboutTask(task: { systemId: string; name: string }) {
    this.askAbout(
      $localize`:Seeds the assistant composer with a question about a task:About task ${task.systemId}:ID: (${task.name}:NAME:): `
    );
  }

  askAboutSprint(sprint: { id: number; name: string }) {
    this.askAbout(
      $localize`:Seeds the assistant composer with a question about a sprint:About sprint ${sprint.name}:NAME: (id ${sprint.id}:ID:): `
    );
  }

  private askAbout(seed: string) {
    if (!this.isAvailable()) {
      return;
    }

    this.panel.open();
    this.setDraft(seed);
  }

  editLastQuestion() {
    const question = this.transcript.lastQuestion();

    if (question === null || this.isStreaming()) {
      return;
    }

    this.setDraft(question);
    this.isReplacingLastTurn.set(true);
  }

  cancelEdit() {
    this.isReplacingLastTurn.set(false);
  }

  async retryLastTurn() {
    await this.send('', true);
  }

  stopTurn() {
    const isRunning = this.isStreaming();

    if (!isRunning || this.isStopping) {
      return;
    }

    this.isStopping = true;

    const conversationId = this.conversationId();

    if (conversationId) {
      void this.api.stopConversation(conversationId);
    }

    this.transcript.markLastStopped();
    this.stream.cancel();
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

    const startedAt = Date.now();

    this.drafts.clearCurrent();
    this.transcript.append({ role: 'user', text: question, tools: [] });
    this.transcript.append({ role: 'assistant', text: '', tools: [] });
    this.isStreaming.set(true);
    this.isThinking.set(true);
    this.pendingTurnAt.set(startedAt);
    this.progress.start(startedAt);

    const token = ++this.turnToken;

    this.isStopping = false;

    try {
      const wasStreamed = await this.stream.run(
        {
          conversationId: this.conversationId(),
          text: question,
          model: this.selectedModel(),
          retry: isRetry,
        },
        (event) => this.receive(event, token)
      );

      if (!wasStreamed) {
        this.failTurn();
      }
    } catch {
      const isCurrent = this.turnToken === token;

      if (isCurrent && !this.isStopping) {
        this.failTurn();
      }
    } finally {
      const isCurrent = this.turnToken === token;

      if (isCurrent) {
        const turnTime = this.progress.stop();

        this.isStreaming.set(false);
        this.isThinking.set(false);
        this.pendingTurnAt.set(null);
        this.isStopping = false;
        this.transcript.markLastDuration(turnTime);
      }
    }

    const isAbandoned = this.turnToken !== token;

    if (isAbandoned) {
      return;
    }

    this.panel.markReplyReceived();

    await this.recoverProposals(token);

    if (wasNewConversation) {
      await this.readGeneratedTitle();
    }
  }

  private rewindToLastQuestion(replacement: string): string | null {
    const question = this.transcript.rewindToLastQuestion(replacement);

    if (question === null) {
      return null;
    }

    this.changeSets.clear();
    this.isReplacingLastTurn.set(false);

    return question;
  }

  private failTurn() {
    this.transcript.failLast(
      $localize`:Shown when the assistant request fails:The assistant could not be reached.`
    );
  }

  private receive(event: AiStreamEvent, token: number) {
    const isAbandoned = this.turnToken !== token;

    if (isAbandoned) {
      return;
    }

    this.applyEvent(event);
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
      this.transcript.appendText(event.text);

      return;
    }

    if (event.type === AiStreamEventType.toolStarted && event.toolName) {
      this.isThinking.set(true);
      this.transcript.appendTool(event.toolName);

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
      this.transcript.resetLast();

      return;
    }

    if (
      event.type === AiStreamEventType.entitiesReferenced &&
      event.references
    ) {
      this.transcript.addReferences(event.references);

      return;
    }

    if (
      event.type === AiStreamEventType.changeSetProposed &&
      event.changeSetId
    ) {
      const token = this.turnToken;

      void this.changeSets.refresh(
        event.changeSetId,
        () => this.turnToken === token
      );

      return;
    }

    if (event.type === AiStreamEventType.historyCompacted) {
      this.transcript.dropMessages(event.droppedMessages ?? 0);

      return;
    }

    if (event.type === AiStreamEventType.usageUpdated && event.usage) {
      this.usage.set(event.usage);

      return;
    }

    if (event.type === AiStreamEventType.turnUsage && event.usage) {
      this.turnUsage.set(event.usage);

      return;
    }

    if (event.type === AiStreamEventType.stopped) {
      this.isThinking.set(false);
      this.transcript.markLastStopped();

      return;
    }

    if (event.type === AiStreamEventType.error) {
      this.isThinking.set(false);
      this.transcript.failLast(
        event.message ??
          $localize`:Shown when the assistant reports a failure:The assistant stopped unexpectedly.`
      );
    }
  }

  private async recoverProposals(token: number) {
    const conversationId = this.conversationId();

    if (conversationId === null) {
      return;
    }

    await this.changeSets.recoverPending(
      conversationId,
      () => this.turnToken === token
    );
  }

  private async readGeneratedTitle() {
    const conversationId = this.conversationId();

    if (!conversationId) {
      return;
    }

    await this.conversation.readGeneratedTitle(conversationId);
  }

  private currentSession(): AiWorkspaceSession {
    return {
      conversationId: this.conversationId(),
      isOpen: this.panel.isOpen(),
      pendingTurnAt: this.pendingTurnAt(),
    };
  }

  private async enterWorkspace(workspace: string, isSwitch: boolean) {
    const session = this.sessions.find(workspace);

    if (isSwitch) {
      this.abandonTurn();
      this.clearConversation();
    }

    try {
      await this.restoreSession(session, isSwitch);
    } finally {
      this.isSwitchingWorkspace = false;

      this.sessions.remember(workspace, this.currentSession());
    }
  }

  private clearConversation() {
    this.transcript.bumpVersion();
    this.conversation.clear();
    this.panel.clearUnreadReply();
    this.isReplacingLastTurn.set(false);
    this.progress.reset();
  }

  private abandonTurn() {
    this.turnToken += 1;

    const hasTurn = this.isStreaming();

    if (!hasTurn) {
      return;
    }

    this.stream.cancel();
    this.isStreaming.set(false);
    this.isThinking.set(false);
    this.pendingTurnAt.set(null);
    this.progress.stop();
  }

  private async restoreSession(
    session: AiWorkspaceSession | null,
    isSwitch: boolean
  ) {
    if (!session) {
      return;
    }

    if (!isSwitch) {
      this.panel.restoreOpen(session.isOpen);
    }

    if (!session.conversationId) {
      return;
    }

    await this.catalog.load();

    const detail = await this.conversation.read(session.conversationId);

    if (!detail) {
      return;
    }

    this.conversation.load(detail);

    const startedAt = session.pendingTurnAt;
    const wasTurnInFlight =
      startedAt !== null && Date.now() - startedAt < RESUME_TIMEOUT;

    if (!wasTurnInFlight) {
      this.pendingTurnAt.set(null);

      return;
    }

    await this.resumeTurn();
  }

  /**
   * A reload drops the event stream, but the server finishes and stores the turn
   * regardless, so wait for the reply to land instead of losing it.
   */
  private async resumeTurn() {
    const conversationId = this.conversationId();
    const startedAt = this.conversation.awaitingReplySince();

    if (conversationId === null || startedAt === null) {
      return;
    }

    const isExpired = Date.now() - startedAt >= RESUME_TIMEOUT;

    if (isExpired) {
      this.pendingTurnAt.set(null);

      return;
    }

    this.transcript.append({ role: 'assistant', text: '', tools: [] });
    this.isStreaming.set(true);
    this.isThinking.set(true);

    // The turn started before this browser did, so the clock counts from the
    // question rather than from the reload.
    this.progress.start(startedAt);

    const token = ++this.turnToken;

    try {
      await this.awaitReply(conversationId, startedAt, token);
    } finally {
      const isCurrent = this.turnToken === token;

      if (isCurrent) {
        this.isStreaming.set(false);
        this.isThinking.set(false);
        this.pendingTurnAt.set(null);
        this.conversation.awaitingReplySince.set(null);
        this.progress.stop();
        this.panel.markReplyReceived();
      }
    }
  }

  private async awaitReply(
    conversationId: string,
    startedAt: number,
    token: number
  ) {
    for (;;) {
      await wait(RESUME_POLL_INTERVAL);

      const isCurrent =
        this.turnToken === token && this.conversationId() === conversationId;

      if (!isCurrent) {
        return;
      }

      const detail = await this.readReply(conversationId);

      if (detail) {
        this.conversation.load(detail);

        return;
      }

      const isExpired = Date.now() - startedAt >= RESUME_TIMEOUT;

      if (isExpired) {
        this.transcript.failLast(
          $localize`:Shown when the assistant reports a failure:The assistant stopped unexpectedly.`
        );

        return;
      }
    }
  }

  private async readReply(
    conversationId: string
  ): Promise<AiConversationDetail | null> {
    const detail = await this.conversation.read(conversationId);
    const messages = detail?.messages ?? [];
    const last = messages[messages.length - 1];
    const hasReply = last !== undefined && last.role !== AiMessageRole.user;

    return hasReply ? detail : null;
  }
}

function wait(duration: number) {
  return new Promise<void>((resolve) => setTimeout(resolve, duration));
}
