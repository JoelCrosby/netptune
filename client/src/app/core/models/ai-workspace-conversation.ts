import { AiProvider } from './ai-credential';
import { AiTokenUsage } from './ai-conversation';

export interface AiWorkspaceConversation {
  id: string;
  title: string;
  userId: string;
  userDisplayName: string;
  userPictureUrl?: string | null;
  provider: AiProvider;
  model: string;
  lastMessageAt: string;
  messageCount: number;
  usage: AiTokenUsage;
}
