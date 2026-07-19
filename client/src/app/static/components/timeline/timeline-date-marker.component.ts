import { Component, computed, input } from '@angular/core';
import { dateOffset, inclusiveDayCount } from './timeline-date-geometry';

@Component({
  selector: 'app-timeline-date-marker',
  host: { class: 'contents' },
  template: `
    @if (visible()) {
      <div
        class="pointer-events-none absolute inset-y-0 z-[5] w-0.5 -translate-x-1/2 bg-red-500 dark:bg-red-400"
        [style.left.px]="left()"
        aria-hidden="true"></div>
    }
  `,
})
export class TimelineDateMarkerComponent {
  readonly date = input.required<string>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly dayWidth = input.required<number>();

  readonly visible = computed(() => {
    const offset = dateOffset(this.from(), this.date());
    return offset >= 0 && offset < inclusiveDayCount(this.from(), this.to());
  });
  readonly left = computed(
    () => (dateOffset(this.from(), this.date()) + 0.5) * this.dayWidth()
  );
}
