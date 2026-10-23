import { Service, inject } from '@angular/core';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';

export interface AiWorkspaceSession {
  conversationId: string | null;
  isOpen: boolean;
  pendingTurnAt: number | null;
}

/** A chat belongs to the workspace it was started in, so sessions are kept per workspace. */
@Service()
export class AiSessionService {
  private readonly settings = inject(BrowserSettingsService);

  find(workspace: string): AiWorkspaceSession | null {
    return this.settings.get('ai.sessions')?.[workspace] ?? null;
  }

  remember(workspace: string, session: AiWorkspaceSession) {
    this.settings.update('ai.sessions', (sessions) => {
      return { ...sessions, [workspace]: session };
    });
  }
}
