import { Service, inject, signal } from '@angular/core';
import { toChatEntries } from '@core/models/ai-chat-entry';
import {
  AiConversation,
  AiConversationDetail,
  AiMessageRole,
} from '@core/models/ai-conversation';
import { hasNextPage } from '@core/models/pagination';
import { AiApiService } from '@core/services/ai-api.service';
import { AiChangeSetService } from '@core/services/ai-change-set.service';
import { AiEffortService } from '@core/services/ai-effort.service';
import { AiModelCatalogService } from '@core/services/ai-model-catalog.service';
import { AiTranscriptService } from '@core/services/ai-transcript.service';

const HISTORY_PAGE_SIZE = 50;

@Service()
export class AiConversationService {
  private readonly api = inject(AiApiService);
  private readonly transcript = inject(AiTranscriptService);
  private readonly changeSets = inject(AiChangeSetService);
  private readonly catalog = inject(AiModelCatalogService);
  private readonly effort = inject(AiEffortService);

  readonly id = signal<string | null>(null);
  readonly title = signal<string | null>(null);
  readonly conversations = signal<AiConversation[]>([]);
  readonly hasMoreConversations = signal(false);
  readonly showHistory = signal(false);

  readonly awaitingReplySince = signal<number | null>(null);

  async read(conversationId: string): Promise<AiConversationDetail | null> {
    return await this.api.readConversation(conversationId);
  }

  private loadedPage = 0;

  async loadList() {
    const page = await this.api.listConversations(1, HISTORY_PAGE_SIZE);

    this.loadedPage = 1;
    this.conversations.set(page?.items ?? []);
    this.hasMoreConversations.set(hasNextPage(page));
  }

  async loadMore() {
    const nextPage = this.loadedPage + 1;
    const page = await this.api.listConversations(nextPage, HISTORY_PAGE_SIZE);

    if (!page) return;

    this.loadedPage = nextPage;
    this.conversations.update((loaded) => {
      const loadedIds = new Set(loaded.map((item) => item.id));
      const unseen = page.items.filter((item) => !loadedIds.has(item.id));

      return [...loaded, ...unseen];
    });
    this.hasMoreConversations.set(hasNextPage(page));
  }

  async toggleHistory() {
    const next = !this.showHistory();

    this.showHistory.set(next);

    if (next) {
      await this.loadList();
    }
  }

  async remove(conversationId: string) {
    await this.api.deleteConversation(conversationId);
  }

  async readGeneratedTitle(conversationId: string) {
    await this.loadList();

    const conversation = this.conversations().find((item) => {
      return item.id === conversationId;
    });

    this.title.set(conversation?.title ?? null);
  }

  async load(detail: AiConversationDetail) {
    const messages = detail.messages;
    const last = messages[messages.length - 1];
    const isAwaitingReply = last?.role === AiMessageRole.user;

    this.id.set(detail.conversation.id);
    this.title.set(detail.conversation.title);
    this.showHistory.set(false);
    this.awaitingReplySince.set(
      isAwaitingReply ? Date.parse(last.createdAt) : null
    );

    this.catalog.use(detail.conversation.requestedModel ?? null);
    this.effort.use(detail.conversation.requestedEffort ?? null);
    this.transcript.setEntries(toChatEntries(messages));
    this.transcript.addReferences(
      messages.flatMap((message) => message.references)
    );
    this.transcript.droppedMessages.set(0);
    this.transcript.usage.set(detail.conversation.usage);
    this.changeSets.set(detail.pendingChangeSet ?? null);

    await this.loadAppliedChangeSets(detail);
  }

  private async loadAppliedChangeSets(detail: AiConversationDetail) {
    const hasOutcome = detail.messages.some((message) => !!message.changeSetId);

    if (!hasOutcome) {
      this.transcript.setAppliedChangeSets([]);

      return;
    }

    const changeSets = await this.api.listConversationChangeSets(
      detail.conversation.id
    );

    this.transcript.setAppliedChangeSets(changeSets);
  }

  startNew() {
    this.reset();
    this.catalog.resetToPreference();
    this.effort.resetToPreference();
  }

  /** A chat from the workspace being left must not follow the user into the next one. */
  clear() {
    this.reset();
    this.loadedPage = 0;
    this.conversations.set([]);
    this.hasMoreConversations.set(false);
  }

  private reset() {
    this.id.set(null);
    this.title.set(null);
    this.showHistory.set(false);
    this.awaitingReplySince.set(null);
    this.transcript.clear();
    this.changeSets.clear();
  }
}
