import { AiTokenUsage } from './ai-conversation';

export interface AiSpendDay {
  day: string;
  cost: number;
}

export interface AiSpendMember {
  userId: string;
  userDisplayName: string;
  conversations: number;
  usage: AiTokenUsage;
}

export interface AiSpend {
  monthToDate: number;
  cap: number | null;
  projected: number;
  periodStart: string;
  periodEnd: string;
  conversations: number;
  usage: AiTokenUsage;
  daily: AiSpendDay[];
  members: AiSpendMember[];
}

export interface SetAiSpendCapRequest {
  cap: number | null;
}
