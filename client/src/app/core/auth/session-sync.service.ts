import {
  EnvironmentProviders,
  Service,
  effect,
  inject,
  provideAppInitializer,
  untracked,
} from '@angular/core';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { SessionService } from '@core/services/session.service';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';

export function provideSessionSync(): EnvironmentProviders {
  return provideAppInitializer(() => {
    inject(SessionSyncService);
  });
}

@Service()
export class SessionSyncService {
  private readonly session = inject(SessionService);
  private readonly authCommands = inject(AuthCommandsService);
  private readonly storedOwner = inject(BrowserSettingsService).storedOwner;

  constructor() {
    effect(() => {
      const owner = this.storedOwner();

      untracked(() => this.follow(owner));
    });
  }

  private follow(owner: string | null) {
    const userId = this.session.currentUserId();

    if (!userId || owner === userId) return;

    if (owner === null) {
      this.authCommands.signOut();

      return;
    }

    window.location.reload();
  }
}
