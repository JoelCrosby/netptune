import {
  Service,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import {
  WORKSPACE_LAST_VISITED,
  WORKSPACE_RECENT_IDS,
} from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';

/** How many workspace ids the recent list keeps, newest first. */
const maxRecentWorkspaces = 3;

@Service()
export class LastWorkspaceService {
  private preferences = inject(UserPreferencesService);

  private currentWorkspace = inject(CurrentWorkspaceService).workspace;
  private isAuthenticated = inject(SessionService).isAuthenticated;
  private lastWritten = signal<string | null>(null);
  private lastRecentWritten = signal<number | null>(null);

  readonly recentIds = computed<number[]>(() => {
    const value = this.preferences.effectiveValueFor(WORKSPACE_RECENT_IDS);

    if (!Array.isArray(value)) return [];

    return value.filter((entry): entry is number => typeof entry === 'number');
  });

  constructor() {
    effect(() => {
      const slug = this.currentWorkspace()?.slug;

      if (!this.isAuthenticated()) return;
      if (!slug || slug === untracked(this.lastWritten)) return;

      untracked(() => this.remember(slug));
    });

    effect(() => {
      const id = this.currentWorkspace()?.id;

      if (!this.isAuthenticated()) return;

      // The new entry is prepended onto the stored list, so writing before the
      // preferences load would truncate the history down to this workspace.
      if (!this.preferences.loaded()) return;
      if (!id || id === untracked(this.lastRecentWritten)) return;

      untracked(() => this.rememberRecent(id));
    });
  }

  private remember(slug: string) {
    this.lastWritten.set(slug);

    this.preferences
      .updateValue(WORKSPACE_LAST_VISITED, 'global', slug)
      .subscribe({
        error: () => this.lastWritten.set(null),
      });
  }

  private rememberRecent(workspaceId: number) {
    const recent = untracked(this.recentIds);

    this.lastRecentWritten.set(workspaceId);

    if (recent[0] === workspaceId) return;

    const next = [
      workspaceId,
      ...recent.filter((id) => id !== workspaceId),
    ].slice(0, maxRecentWorkspaces);

    this.preferences
      .updateValue(WORKSPACE_RECENT_IDS, 'global', next)
      .subscribe({
        error: () => this.lastRecentWritten.set(null),
      });
  }
}
