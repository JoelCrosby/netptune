import { computed, inject, Injectable, signal } from '@angular/core';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { LayoutService } from '@core/services/layout.service';

@Injectable()
export class ShellService {
  private storage = inject(LocalStorageService);
  private layout = inject(LayoutService);

  private readonly expandedPreference = signal<boolean>(
    this.storage.getItem('side-nav-expanded') ?? true
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
    this.storage.setItem('side-nav-expanded', expanded);
  }
}
