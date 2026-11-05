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
  reviewModel?: string | null;
  createdAt: string;
  lastUsedAt?: string | null;
}

export interface SaveAiCredentialRequest {
  provider: AiProvider;
  label: string;
  secret: string;
  model?: string | null;
  reviewModel?: string | null;
}

export interface UpdateAiCredentialModelsRequest {
  model: string | null;
  reviewModel: string | null;
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

export interface AiProviderDescriptor {
  provider: AiProvider;
  label: string;
  hint: string;
}

export const AI_PROVIDERS: AiProviderDescriptor[] = [
  {
    provider: AiProvider.anthropic,
    label: $localize`:Name of the Anthropic AI provider:Anthropic`,
    hint: $localize`:Where to find an Anthropic API key:Create a key at console.anthropic.com`,
  },
  {
    provider: AiProvider.openAi,
    label: $localize`:Name of the OpenAI provider:OpenAI`,
    hint: $localize`:Where to find an OpenAI API key:Create a key at platform.openai.com`,
  },
];
