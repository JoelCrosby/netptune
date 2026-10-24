import { Service, effect, inject, signal, untracked } from '@angular/core';
import { APPEARANCE_LOCALE } from '@core/models/user-preferences';
import { LocaleService } from '@core/services/locale.service';
import { SessionService } from '@core/services/session.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';

// The server reads this preference to write e-mail and other background messages in the
// user's language, since those have no request, and so no locale cookie, to go on.
@Service()
export class LocalePreferenceService {
  private preferences = inject(UserPreferencesService);
  private isAuthenticated = inject(SessionService).isAuthenticated;
  private locale = inject(LocaleService).current.code;
  private written = signal<string | null>(null);

  constructor() {
    effect(() => {
      if (!this.isAuthenticated()) return;
      if (!this.preferences.loaded()) return;

      const stored = this.preferences.effectiveValueFor(APPEARANCE_LOCALE);
      const isStored = stored === this.locale;
      const isAlreadySent = untracked(this.written) === this.locale;

      if (isStored || isAlreadySent) return;

      untracked(() => this.remember());
    });
  }

  private remember() {
    this.written.set(this.locale);

    this.preferences
      .updateValue(APPEARANCE_LOCALE, 'global', this.locale)
      .subscribe({
        error: () => this.written.set(null),
      });
  }
}
