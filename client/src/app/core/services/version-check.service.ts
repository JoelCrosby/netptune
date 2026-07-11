import { DOCUMENT } from '@angular/common';
import {
  EnvironmentProviders,
  Injectable,
  NgZone,
  provideAppInitializer,
  inject,
} from '@angular/core';
import { environment } from '@env/environment';
import { BannerService } from '@static/components/banner/banner.service';

const POLL_INTERVAL = 5 * 60 * 1000;

export function provideVersionCheck(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const service = inject(VersionCheckService);

    if (environment.production) {
      service.listen();
    } else {
      service.exposeDevTrigger();
    }
  });
}

@Injectable({ providedIn: 'root' })
export class VersionCheckService {
  private readonly document = inject(DOCUMENT);
  private readonly zone = inject(NgZone);
  private readonly banner = inject(BannerService);

  private baseline: string | null = null;
  private prompted = false;
  private checking = false;

  exposeDevTrigger(): void {
    const view = this.document.defaultView as
      | (Window & { triggerVersionBanner?: () => void })
      | null;

    if (view == null) {
      return;
    }

    view.triggerVersionBanner = () => this.zone.run(() => this.promptRefresh());

    console.info(
      '[version-check] call triggerVersionBanner() to preview the update banner.'
    );
  }

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

    this.banner.show('A new version of Netptune is available.', {
      action: 'Reload',
      dismissible: false,
      onAction: () => void this.hardReload(),
    });
  }

  private async hardReload(): Promise<void> {
    const view = this.document.defaultView;

    if (view == null) {
      return;
    }

    // Drop any cached assets (e.g. from a service worker / the Cache API) so
    // the freshly deployed document and hashed bundles are fetched from the
    // network rather than served from cache.
    if ('caches' in view) {
      try {
        const keys = await view.caches.keys();
        await Promise.all(keys.map((key) => view.caches.delete(key)));
      } catch {
        // Ignore cache eviction failures and fall through to the reload.
      }
    }

    view.location.reload();
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
