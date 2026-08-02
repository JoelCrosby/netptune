export enum AiProvider {
  anthropic = 0,
  openAi = 1,
}

export interface AiCredential {
  id: string;
  provider: AiProvider;
  label: string;
  secretHint: string;
  model?: string | null;
  createdAt: string;
  lastUsedAt?: string | null;
}

export interface SaveAiCredentialRequest {
  provider: AiProvider;
  label: string;
  secret: string;
  model?: string | null;
}

export type AiCredentialScope = 'user' | 'workspace';

export enum AiCredentialSource {
  user = 0,
  workspace = 1,
}

export interface AiProviderAvailability {
  provider: AiProvider;
  source: AiCredentialSource;
  model?: string | null;
}

export interface AiCredentialAvailability {
  providers: AiProviderAvailability[];
}
