import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { injectParams } from '@app/core/router/signals';
import { parseTaskFilterRouteParams } from '@app/core/router/task-filter-route-params';
import { LucideFlag } from '@lucide/angular';

@Component({
  selector: 'app-task-list-flags',
  imports: [LucideFlag],
  template: `
    <button
      type="button"
      class="border-border hover:bg-foreground/5 flex h-9 items-center gap-2 rounded-md border px-3 text-sm transition-colors"
      [class.border-amber-400]="selected()"
      [class.bg-amber-400/10]="selected()"
      [class.text-amber-700]="selected()"
      [attr.aria-pressed]="selected()"
      (click)="toggle()">
      <svg lucideFlag size="16" aria-hidden="true"></svg>
      Flagged
    </button>
  `,
})
export class TaskListFlagsComponent {
  private readonly router = inject(Router);
  private readonly params = injectParams();

  readonly selected = computed(
    () => parseTaskFilterRouteParams(this.params()).hasFlags === true
  );

  toggle() {
    void this.router.navigate([], {
      queryParams: {
        hasFlags: this.selected() ? null : true,
      },
      queryParamsHandling: 'merge',
    });
  }
}
