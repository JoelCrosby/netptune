import { Component, computed, effect, input } from '@angular/core';
import { Status, StatusCategory } from '@core/models/status';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { LucideCheck } from '@lucide/angular';

@Component({
  selector: 'app-board-group-status-dot',
  imports: [TooltipDirective, LucideCheck],
  template: `
    @if (isCompleted()) {
      <svg
        lucideCheck
        [style.color]="status().color || 'green'"
        class="h-4 w-4"></svg>
    } @else {
      <span
        class="ml-[.4rem] block h-2.5 w-2.5 flex-none rounded-full"
        [style.background-color]="status().color || 'var(--primary)'"
        [appTooltip]="toolTip()"></span>
    }
  `,
})
export class BoardGroupStatusDotComponent {
  readonly status = input.required<Status>();

  isCompleted = computed(() => this.status().category === StatusCategory.done);

  toolTip = computed(() => {
    return `Tasks moved into this group will be set to ${this.status().name}`;
  });

  constructor() {
    effect(() => console.log('status: ', this.status()));
  }
}
