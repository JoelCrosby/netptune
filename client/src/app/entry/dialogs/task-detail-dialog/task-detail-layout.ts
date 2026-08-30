import { computed, inject, Service } from '@angular/core';
import { APPEARANCE_TASK_DETAIL_LAYOUT } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';

export type TaskDetailLayout = 'summary-rail' | 'cockpit' | 'document';

export const DEFAULT_TASK_DETAIL_LAYOUT: TaskDetailLayout = 'summary-rail';

const layouts = new Set<string>(['summary-rail', 'cockpit', 'document']);

function isTaskDetailLayout(value: unknown): value is TaskDetailLayout {
  return typeof value === 'string' && layouts.has(value);
}

@Service()
export class TaskDetailLayoutService {
  private readonly preferences = inject(UserPreferencesService);

  readonly layout = computed<TaskDetailLayout>(() => {
    const value = this.preferences.effectiveValueFor(
      APPEARANCE_TASK_DETAIL_LAYOUT
    );

    return isTaskDetailLayout(value) ? value : DEFAULT_TASK_DETAIL_LAYOUT;
  });

  constructor() {
    this.preferences.ensureLoaded();
  }
}
