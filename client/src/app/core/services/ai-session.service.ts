import { Service, inject } from '@angular/core';
import { LocalStorageService } from '@core/local-storage/local-storage.service';

export interface AiWorkspaceSession {
  conversationId: string | null;
  isOpen: boolean;
  pendingTurnAt: number | null;
}

/** A chat belongs to the workspace it was started in, so sessions are kept per workspace. */
type AiStoredSessions = Record<string, AiWorkspaceSession>;

const SESSION_STORAGE_KEY = 'ai-assistant.sessions';
const LEGACY_SESSION_STORAGE_KEY = 'ai-assistant.session';

@Service()
export class AiSessionService {
  private readonly storage = inject(LocalStorageService);

  private sessions: AiStoredSessions = this.read();

  find(workspace: string): AiWorkspaceSession | null {
    return this.sessions[workspace] ?? null;
  }

  remember(workspace: string, session: AiWorkspaceSession) {
    this.sessions = { ...this.sessions, [workspace]: session };

    this.storage.setItem(SESSION_STORAGE_KEY, this.sessions);
  }

  private read(): AiStoredSessions {
    this.storage.removeItem(LEGACY_SESSION_STORAGE_KEY);

    return this.storage.getItem<AiStoredSessions>(SESSION_STORAGE_KEY) ?? {};
  }
}
