import { Service, computed, inject } from '@angular/core';
import { WORKSPACES_PINNED_IDS } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';

@Service()
export class PinnedWorkspacesService {
  private readonly preferences = inject(UserPreferencesService);

  readonly pinnedIds = computed<number[]>(() => {
    const value = this.preferences.effectiveValueFor(WORKSPACES_PINNED_IDS);

    if (!Array.isArray(value)) return [];

    return value.filter((entry): entry is number => typeof entry === 'number');
  });

  isPinned(workspaceId: number): boolean {
    return this.pinnedIds().includes(workspaceId);
  }

  toggle(workspaceId: number) {
    const pinned = this.pinnedIds();
    const next = pinned.includes(workspaceId)
      ? pinned.filter((id) => id !== workspaceId)
      : [...pinned, workspaceId];

    this.preferences
      .updateValue(WORKSPACES_PINNED_IDS, 'global', next)
      .subscribe({ error: () => undefined });
  }
}
