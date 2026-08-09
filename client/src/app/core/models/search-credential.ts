export enum WebSearchProvider {
  brave = 0,
  google = 1,
  searxng = 2,
}

export interface SearchCredential {
  id: string;
  provider: WebSearchProvider;
  secretHint: string;
  engineId?: string | null;
  endpoint?: string | null;
  createdAt: string;
  lastUsedAt?: string | null;
}

export interface SaveSearchCredentialRequest {
  provider: WebSearchProvider;
  secret?: string | null;
  engineId?: string | null;
  endpoint?: string | null;
}
