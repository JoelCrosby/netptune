import { DOCUMENT } from '@angular/common';
import {
  EnvironmentProviders,
  Injectable,
  NgZone,
  provideAppInitializer,
  inject,
} from '@angular/core';
import { environment } from '@env/environment';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';

const POLL_INTERVAL = 5 * 60 * 1000;

export function provideVersionCheck(): EnvironmentProviders {
  return provideAppInitializer(() => {
    if (environment.production) {
      inject(VersionCheckService).listen();
    }
  });
}

@Injectable({ providedIn: 'root' })
export class VersionCheckService {
  private readonly document = inject(DOCUMENT);
  private readonly zone = inject(NgZone);
  private readonly snackbar = inject(SnackbarService);

  private baseline: string | null = null;
  private prompted = false;
  private checking = false;

  async listen(): Promise<void> {
    this.baseline = await this.fetchSignature();

    this.zone.runOutsideAngular(() => {
      setInterval(() => this.check(), POLL_INTERVAL);

      this.document.addEventListener('visibilitychange', () => {
        if (this.document.visibilityState === 'visible') {
          this.check();
        }
      });
    });
  }

  private async check(): Promise<void> {
    if (this.prompted || this.checking || this.baseline == null) {
      return;
    }

    this.checking = true;

    try {
      const current = await this.fetchSignature();

      if (current != null && current !== this.baseline) {
        this.zone.run(() => this.promptRefresh());
      }
    } finally {
      this.checking = false;
    }
  }

  private promptRefresh(): void {
    this.prompted = true;

    this.snackbar.open('A new version is available.', 'Refresh', {
      type: 'info',
      duration: 0,
      onAction: () => this.document.defaultView?.location.reload(),
    });
  }

  private async fetchSignature(): Promise<string | null> {
    try {
      const url = `index.html?_=${Date.now()}`;
      const response = await fetch(url, { cache: 'no-store' });

      if (!response.ok) {
        return null;
      }

      const html = await response.text();
      const matches = html.match(
        /(?:src|href)="[^"]*-[A-Z0-9]{8,}\.(?:js|css)"/gi
      );

      if (matches == null || matches.length === 0) {
        return null;
      }

      return matches.sort().join('|');
    } catch {
      return null;
    }
  }
}
