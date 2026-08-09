import { AiMessage, AiMessageRole } from '@core/models/ai-conversation';

export interface AiChatEntry {
  role: 'user' | 'assistant';
  text: string;
  tools: string[];
  /** How long the reply took, so it can say how long it thought. */
  durationMs?: number;
  failed?: boolean;
  stopped?: boolean;
}

/**
 * Shared so a stored transcript renders the same wherever it is read back.
 *
 * A stored turn records no duration of its own, so a reply is timed from the
 * question it answers — the question is written when the turn starts and the
 * reply when it ends.
 */
export const toChatEntries = (messages: AiMessage[]): AiChatEntry[] => {
  let askedAt: number | null = null;

  return messages.map((message) => {
    const isUser = message.role === AiMessageRole.user;
    const writtenAt = Date.parse(message.createdAt);

    if (isUser) {
      askedAt = writtenAt;

      return {
        role: 'user',
        text: message.text ?? '',
        tools: message.toolNames,
      };
    }

    const durationMs =
      askedAt === null ? undefined : Math.max(0, writtenAt - askedAt);

    askedAt = null;

    return {
      role: 'assistant',
      text: message.text ?? '',
      tools: message.toolNames,
      durationMs,
    };
  });
};
