import { Service, inject, signal } from '@angular/core';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';

@Service()
export class PageRailService {
  private readonly settings = inject(BrowserSettingsService);

  readonly collapsed = signal(
    this.settings.get('pages.railCollapsed') === true
  );

  setCollapsed(collapsed: boolean) {
    this.collapsed.set(collapsed);
    this.settings.set('pages.railCollapsed', collapsed);
  }
}
