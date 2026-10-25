import { computed, inject, Service, signal } from '@angular/core';
import { APPEARANCE_TASK_DETAIL_LAYOUT } from '@core/models/user-preferences';
import { LayoutService } from '@core/services/layout.service';
import { SessionService } from '@core/services/session.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { catchError, EMPTY, finalize } from 'rxjs';

export type TaskDetailLayout = 'cockpit' | 'document';

export const DEFAULT_TASK_DETAIL_LAYOUT: TaskDetailLayout = 'cockpit';

const layouts = new Set<string>(['cockpit', 'document']);

function isTaskDetailLayout(value: unknown): value is TaskDetailLayout {
  return typeof value === 'string' && layouts.has(value);
}

@Service()
export class TaskDetailLayoutService {
  private readonly preferences = inject(UserPreferencesService);
  private readonly isMobileView = inject(LayoutService).isMobileView;
  private readonly authenticated = inject(SessionService).isAuthenticated;

  private readonly pending = signal<TaskDetailLayout | null>(null);

  readonly layout = computed<TaskDetailLayout>(() => {
    if (this.isMobileView()) return 'document';

    const pending = this.pending();

    if (pending) return pending;

    const value = this.preferences.effectiveValueFor(
      APPEARANCE_TASK_DETAIL_LAYOUT
    );

    return isTaskDetailLayout(value) ? value : DEFAULT_TASK_DETAIL_LAYOUT;
  });

  readonly otherLayout = computed<TaskDetailLayout>(() => {
    return this.layout() === 'document' ? 'cockpit' : 'document';
  });

  readonly canSwitch = computed(() => {
    return this.authenticated() && !this.isMobileView();
  });

  constructor() {
    this.preferences.ensureLoaded();
  }

  toggle() {
    this.switchTo(this.otherLayout());
  }

  switchTo(layout: TaskDetailLayout) {
    const writeInFlight = this.pending() !== null;

    if (writeInFlight) return;

    this.pending.set(layout);

    this.preferences
      .updateValue(APPEARANCE_TASK_DETAIL_LAYOUT, 'global', layout)
      .pipe(
        catchError(() => EMPTY),
        finalize(() => this.pending.set(null))
      )
      .subscribe();
  }
}
