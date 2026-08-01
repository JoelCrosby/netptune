import { AiProvider } from './ai-credential';

export enum AiMessageRole {
  user = 0,
  assistant = 1,
  tool = 2,
}

export enum AiStreamEventType {
  textDelta = 0,
  toolStarted = 1,
  toolCompleted = 2,
  turnCompleted = 3,
  error = 4,
  conversationStarted = 5,
}

export interface AiStreamEvent {
  type: AiStreamEventType;
  text?: string;
  toolName?: string;
  message?: string;
  conversationId?: string;
}

export interface AiConversation {
  id: string;
  title: string;
  provider: AiProvider;
  model: string;
  lastMessageAt: string;
  messageCount: number;
}

export interface AiMessage {
  id: number;
  sequence: number;
  role: AiMessageRole;
  text?: string;
  toolNames: string[];
  createdAt: string;
}

export interface AiConversationDetail {
  conversation: AiConversation;
  messages: AiMessage[];
}
