import { Service, signal } from '@angular/core';
import { AiChatEntry } from '@core/models/ai-chat-entry';
import {
  AiChangeSet,
  AiEntityReference,
  AiTokenUsage,
} from '@core/models/ai-conversation';
import { referenceKey } from '@core/util/ai-references';

@Service()
export class AiTranscriptService {
  readonly entries = signal<AiChatEntry[]>([]);
  readonly references = signal<Map<string, AiEntityReference>>(new Map());
  readonly appliedChangeSets = signal<Map<string, AiChangeSet>>(new Map());
  readonly droppedMessages = signal(0);
  readonly usage = signal<AiTokenUsage | null>(null);

  readonly version = signal(0);

  setEntries(entries: AiChatEntry[]) {
    this.entries.set(entries);
  }

  clear() {
    this.entries.set([]);
    this.references.set(new Map());
    this.appliedChangeSets.set(new Map());
    this.droppedMessages.set(0);
    this.usage.set(null);
  }

  setAppliedChangeSets(changeSets: AiChangeSet[]) {
    const byId = changeSets.map((changeSet): [string, AiChangeSet] => {
      return [changeSet.id, changeSet];
    });

    this.appliedChangeSets.set(new Map(byId));
  }

  bumpVersion() {
    this.version.update((version) => version + 1);
  }

  dropMessages(count: number) {
    this.droppedMessages.update((current) => current + count);
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

  append(entry: AiChatEntry) {
    this.entries.update((current) => [...current, entry]);
  }

  appendText(text: string) {
    this.updateLast((last) => ({ ...last, text: last.text + text }));
  }

  appendTool(toolName: string) {
    this.updateLast((last) => ({ ...last, tools: [...last.tools, toolName] }));
  }

  /** The harness discards a reply it caught claiming work it never did. */
  resetLast() {
    this.updateLast((last) => ({ ...last, text: '', tools: [] }));
  }

  failLast(message: string) {
    this.updateLast((last) => ({ ...last, text: message, failed: true }));
  }

  markLastStopped() {
    this.updateReply((last) => ({ ...last, stopped: true }));
  }

  markLastDuration(durationMs: number) {
    this.updateReply((last) => ({ ...last, durationMs }));
  }

  lastQuestion(): string | null {
    const entries = this.entries();

    for (let index = entries.length - 1; index >= 0; index -= 1) {
      const entry = entries[index];

      if (isQuestion(entry)) {
        return entry.text;
      }
    }

    return null;
  }

  /** Drops the exchange being replaced, and answers with the question to ask again. */
  rewindToLastQuestion(replacement: string): string | null {
    const entries = this.entries();
    let index = entries.length - 1;

    while (index >= 0 && !isQuestion(entries[index])) {
      index -= 1;
    }

    if (index < 0) {
      return null;
    }

    const question = replacement.length > 0 ? replacement : entries[index].text;

    this.entries.set(entries.slice(0, index));

    return question;
  }

  private updateLast(change: (entry: AiChatEntry) => AiChatEntry) {
    this.entries.update((current) => {
      const next = [...current];
      const last = next[next.length - 1];

      next[next.length - 1] = change(last);

      return next;
    });
  }

  private updateReply(change: (entry: AiChatEntry) => AiChatEntry) {
    this.entries.update((current) => {
      const next = [...current];
      const last = next[next.length - 1];

      if (!last || last.role !== 'assistant') {
        return current;
      }

      next[next.length - 1] = change(last);

      return next;
    });
  }
}

/** The record of an applied change set is written as a user message, but it is not something to ask again. */
function isQuestion(entry: AiChatEntry): boolean {
  return entry.role === 'user' && entry.changeSetId === undefined;
}
