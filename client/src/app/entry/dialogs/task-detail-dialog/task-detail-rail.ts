import { computed, inject, linkedSignal, Service } from '@angular/core';
import { APPEARANCE_TASK_DETAIL_RAIL_COLLAPSED } from '@core/models/user-preferences';
import { SessionService } from '@core/services/session.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';

@Service()
export class TaskDetailRailService {
  private readonly preferences = inject(UserPreferencesService);
  private readonly isAuthenticated = inject(SessionService).isAuthenticated;

  private readonly saved = computed(() => {
    const value = this.preferences.effectiveValueFor(
      APPEARANCE_TASK_DETAIL_RAIL_COLLAPSED
    );

    return value === true;
  });

  readonly collapsed = linkedSignal(() => this.saved());

  constructor() {
    this.preferences.ensureLoaded();
  }

  setCollapsed(collapsed: boolean) {
    this.collapsed.set(collapsed);

    if (!this.isAuthenticated()) return;

    this.preferences
      .updateValue(APPEARANCE_TASK_DETAIL_RAIL_COLLAPSED, 'global', collapsed)
      .subscribe({ error: () => undefined });
  }
}
