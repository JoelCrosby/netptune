import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-timeline-weekend-shading',
  host: { class: 'contents' },
  template: `
    <span
      class="pointer-events-none absolute inset-0 bg-repeat-x"
      [style.background-image]="backgroundImage"
      [style.background-position-x.px]="backgroundOffset()"
      [style.background-size]="backgroundSize()"
      [attr.aria-hidden]="true"></span>
  `,
})
export class TimelineWeekendShadingComponent {
  readonly from = input.required<string>();
  readonly dayWidth = input.required<number>();
  readonly backgroundImage =
    'linear-gradient(to right, color-mix(in srgb, currentColor 5%, transparent) 0 28.571%, transparent 28.571% 100%)';
  readonly backgroundSize = computed(() => `${this.dayWidth() * 7}px 100%`);
  readonly backgroundOffset = computed(() => {
    const startDay = new Date(`${this.from()}T00:00:00Z`).getUTCDay();
    const firstSaturdayOffset = startDay === 0 ? -1 : (6 - startDay + 7) % 7;

    return firstSaturdayOffset * this.dayWidth();
  });
}
