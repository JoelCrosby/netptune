import { computed, inject, Service } from '@angular/core';
import { APPEARANCE_PAGE_WIDTH } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';

export type PageWidth = 'centered' | 'full';

export const DEFAULT_PAGE_WIDTH: PageWidth = 'centered';

const widths = new Set<string>(['centered', 'full']);

function isPageWidth(value: unknown): value is PageWidth {
  return typeof value === 'string' && widths.has(value);
}

// How wide the pages that fill their width run — a centred column capped at a
// readable width, or the full window. Read by PageContainerComponent and the
// bands that align to it.
@Service()
export class PageWidthService {
  private readonly preferences = inject(UserPreferencesService);

  readonly width = computed<PageWidth>(() => {
    const value = this.preferences.effectiveValueFor(APPEARANCE_PAGE_WIDTH);

    return isPageWidth(value) ? value : DEFAULT_PAGE_WIDTH;
  });

  readonly centered = computed(() => this.width() === 'centered');

  constructor() {
    this.preferences.ensureLoaded();
  }
}
