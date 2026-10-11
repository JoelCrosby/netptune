import { computed, inject, linkedSignal, Service } from '@angular/core';
import { APPEARANCE_PAGE_CONTENT_WIDTH } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';

export type PageContentWidth = 'centered' | 'full';

const widths = new Set<string>(['centered', 'full']);

function isPageContentWidth(value: unknown): value is PageContentWidth {
  return typeof value === 'string' && widths.has(value);
}

@Service()
export class PageContentWidthService {
  private readonly preferences = inject(UserPreferencesService);

  private readonly saved = computed<PageContentWidth>(() => {
    const value = this.preferences.effectiveValueFor(
      APPEARANCE_PAGE_CONTENT_WIDTH
    );

    return isPageContentWidth(value) ? value : 'centered';
  });

  // Flips straight away rather than after the preference round trip.
  readonly width = linkedSignal(() => this.saved());

  constructor() {
    this.preferences.ensureLoaded();
  }

  set(width: PageContentWidth) {
    this.width.set(width);
    this.preferences
      .updateValue(APPEARANCE_PAGE_CONTENT_WIDTH, 'global', width)
      .subscribe();
  }
}
