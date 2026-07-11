import { Injectable, effect, inject, signal, untracked } from '@angular/core';
import { WORKSPACE_LAST_VISITED } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';

@Injectable({ providedIn: 'root' })
export class LastWorkspaceService {
  private store = inject(Store);
  private preferences = inject(UserPreferencesService);

  private currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);
  private lastWritten = signal<string | null>(null);

  constructor() {
    effect(() => {
      const slug = this.currentWorkspace()?.slug;

      if (!slug || slug === untracked(this.lastWritten)) return;

      untracked(() => this.remember(slug));
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
}
