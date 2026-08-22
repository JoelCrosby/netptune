import { Service, computed, inject } from '@angular/core';
import { VIEWS_PINNED_IDS } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';

@Service()
export class PinnedViewsService {
  private readonly preferences = inject(UserPreferencesService);

  readonly pinnedIds = computed<number[]>(() => {
    const value = this.preferences.effectiveValueFor(VIEWS_PINNED_IDS);

    if (!Array.isArray(value)) return [];

    return value.filter((entry): entry is number => typeof entry === 'number');
  });

  isPinned(viewId: number): boolean {
    return this.pinnedIds().includes(viewId);
  }

  unpin(viewId: number) {
    const pinned = this.pinnedIds();

    if (!pinned.includes(viewId)) return;

    this.save(pinned.filter((id) => id !== viewId));
  }

  toggle(viewId: number) {
    const pinned = this.pinnedIds();
    const next = pinned.includes(viewId)
      ? pinned.filter((id) => id !== viewId)
      : [...pinned, viewId];

    this.save(next);
  }

  private save(pinnedIds: number[]) {
    this.preferences
      .updateValue(VIEWS_PINNED_IDS, 'workspace', pinnedIds)
      .subscribe({ error: () => undefined });
  }
}
