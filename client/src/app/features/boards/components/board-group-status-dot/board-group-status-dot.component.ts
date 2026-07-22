import { Component, computed, input } from '@angular/core';
import { Status, StatusCategory } from '@core/models/status';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { LucideCheck } from '@lucide/angular';
import { colorSwatchClass, colorTextClass } from '@core/util/colors/colors';

@Component({
  selector: 'app-board-group-status-dot',
  imports: [TooltipDirective, LucideCheck],
  template: `
    @if (isCompleted()) {
      <svg lucideCheck [class]="'h-4 w-4 ' + statusTextClass()"></svg>
    } @else {
      <span
        [class]="
          'ml-[.4rem] block h-2.5 w-2.5 flex-none rounded-full ' +
          statusSwatchClass()
        "
        [appTooltip]="toolTip()"></span>
    }
  `,
})
export class BoardGroupStatusDotComponent {
  readonly status = input.required<Status>();

  isCompleted = computed(() => this.status().category === StatusCategory.done);
  statusSwatchClass = computed(() => colorSwatchClass(this.status().color));
  statusTextClass = computed(() => colorTextClass(this.status().color));

  toolTip = computed(() => {
    return `Tasks moved into this group will be set to ${this.status().name}`;
  });
}
