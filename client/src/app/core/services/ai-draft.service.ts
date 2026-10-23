import { Service, computed, effect, inject, signal } from '@angular/core';
import { AiConversationService } from '@core/services/ai-conversation.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';

const DRAFT_PERSIST_DELAY = 400;
const NEW_CONVERSATION_KEY = 'new';

@Service()
export class AiDraftService {
  private readonly settings = inject(BrowserSettingsService);
  private readonly workspaceId = inject(CurrentWorkspaceService).slug;
  private readonly conversation = inject(AiConversationService);

  readonly text = signal('');

  /** Drafts follow the chat they were typed in, so switching chats swaps them. */
  readonly key = computed(() => {
    const conversationId = this.conversation.id() ?? NEW_CONVERSATION_KEY;

    return this.keyFor(conversationId);
  });

  private unsaved = new Map<string, string>();
  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      const key = this.key();

      this.text.set(this.draftFor(key));
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

  private draftFor(key: string): string {
    return this.unsaved.get(key) ?? this.settings.get('ai.drafts')?.[key] ?? '';
  }

  private remember(key: string, text: string) {
    this.unsaved.set(key, text);

    if (this.timer !== null) {
      clearTimeout(this.timer);
    }

    this.timer = setTimeout(() => {
      this.timer = null;
      this.flush();
    }, DRAFT_PERSIST_DELAY);
  }

  private flush() {
    const unsaved = [...this.unsaved];

    this.unsaved = new Map();
    this.settings.update('ai.drafts', (drafts) => {
      return unsaved.reduce(withDraft, drafts ?? {});
    });
  }
}

function withDraft(
  drafts: Record<string, string>,
  [key, text]: [string, string]
): Record<string, string> {
  const hasText = text.trim().length > 0;

  if (hasText) {
    return { ...drafts, [key]: text };
  }

  const remaining = Object.entries(drafts).filter(([stored]) => {
    return stored !== key;
  });

  return Object.fromEntries(remaining);
}
