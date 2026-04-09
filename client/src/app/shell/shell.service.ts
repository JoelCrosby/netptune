import { computed, inject, Injectable, signal } from '@angular/core';
import { LocalStorageService } from '@core/local-storage/local-storage.service';

@Injectable()
export class ShellService {
  private storage = inject(LocalStorageService);

  sideNavExpanded = signal<boolean>(
    this.storage.getItem('side-nav-expanded') ?? true
  );

  sideNavCollapsed = computed(() => !this.sideNavExpanded());

  toggleSidebar() {
    this.sideNavExpanded.set(!this.sideNavExpanded());
    this.storage.setItem('side-nav-expanded', this.sideNavExpanded());
  }
}
