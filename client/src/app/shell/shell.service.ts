import { computed, inject, Injectable, signal } from '@angular/core';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';
import { LayoutService } from '@core/services/layout.service';

@Injectable()
export class ShellService {
  private settings = inject(BrowserSettingsService);
  private layout = inject(LayoutService);

  private readonly expandedPreference = signal<boolean>(
    this.settings.get('shell.sideNavExpanded') ?? true
  );

  readonly isDrawer = this.layout.isMobileView;

  readonly isRail = computed(() => {
    return this.layout.isCompactView() && !this.layout.isMobileView();
  });

  sideNavExpanded = computed(() => {
    if (this.isDrawer()) return true;
    if (this.isRail()) return false;

    return this.expandedPreference();
  });

  sideNavCollapsed = computed(() => !this.sideNavExpanded());

  toggleSidebar() {
    const expanded = !this.expandedPreference();

    this.expandedPreference.set(expanded);
    this.settings.set('shell.sideNavExpanded', expanded);
  }
}
