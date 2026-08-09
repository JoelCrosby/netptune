import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { AiConversationService } from '@core/services/ai-conversation.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';

const DRAFT_STORAGE_KEY = 'ai-assistant.drafts';
const DRAFT_PERSIST_DELAY = 400;
const NEW_CONVERSATION_KEY = 'new';

@Injectable({ providedIn: 'root' })
export class AiDraftService {
  private readonly storage = inject(LocalStorageService);
  private readonly workspaceId = inject(CurrentWorkspaceService).slug;
  private readonly conversation = inject(AiConversationService);

  readonly text = signal('');

  /** Drafts follow the chat they were typed in, so switching chats swaps them. */
  readonly key = computed(() => {
    const conversationId = this.conversation.id() ?? NEW_CONVERSATION_KEY;

    return this.keyFor(conversationId);
  });

  private drafts: Record<string, string> =
    this.storage.getItem<Record<string, string>>(DRAFT_STORAGE_KEY) ?? {};

  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      const key = this.key();

      this.text.set(this.drafts[key] ?? '');
    });
  }

  set(text: string) {
    this.text.set(text);
    this.remember(this.key(), text);
  }

  clearCurrent() {
    this.clear(this.key());
  }

  clearNew() {
    this.clear(this.keyFor(NEW_CONVERSATION_KEY));
  }

  discard(conversationId: string) {
    this.remember(this.keyFor(conversationId), '');
  }

  private clear(key: string) {
    this.text.set('');
    this.remember(key, '');
  }

  private keyFor(conversationId: string): string {
    return `${this.workspaceId() ?? ''}:${conversationId}`;
  }

  private remember(key: string, text: string) {
    this.drafts = this.withDraft(key, text);

    if (this.timer !== null) {
      clearTimeout(this.timer);
    }

    this.timer = setTimeout(() => {
      this.timer = null;
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
}
