import { Service, computed, inject } from '@angular/core';
import { QUERIES_PINNED_IDS } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';

@Service()
export class PinnedQueriesService {
  private readonly preferences = inject(UserPreferencesService);

  readonly pinnedIds = computed<number[]>(() => {
    const value = this.preferences.effectiveValueFor(QUERIES_PINNED_IDS);

    if (!Array.isArray(value)) return [];

    return value.filter((entry): entry is number => typeof entry === 'number');
  });

  isPinned(queryId: number): boolean {
    return this.pinnedIds().includes(queryId);
  }

  unpin(queryId: number) {
    const pinned = this.pinnedIds();

    if (!pinned.includes(queryId)) return;

    this.save(pinned.filter((id) => id !== queryId));
  }

  toggle(queryId: number) {
    const pinned = this.pinnedIds();
    const next = pinned.includes(queryId)
      ? pinned.filter((id) => id !== queryId)
      : [...pinned, queryId];

    this.save(next);
  }

  private save(pinnedIds: number[]) {
    this.preferences
      .updateValue(QUERIES_PINNED_IDS, 'workspace', pinnedIds)
      .subscribe({ error: () => undefined });
  }
}
