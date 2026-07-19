import { Component, computed, input, output } from '@angular/core';
import { clippedRangeLeft, clippedRangeWidth } from './timeline-date-geometry';

@Component({
  selector: 'app-timeline-bar',
  host: { class: 'contents' },
  template: `
    @if (milestone()) {
      <button
        type="button"
        class="absolute top-3 h-5 w-5 rotate-45 cursor-pointer rounded-sm border border-white/60 bg-blue-600 shadow"
        [style.left.px]="left()"
        [attr.aria-label]="accessibleLabel()"
        (click)="activated.emit()"></button>
    } @else {
      <button
        type="button"
        class="absolute top-2 h-7 min-w-2 cursor-pointer overflow-hidden rounded bg-blue-600 px-2 text-left text-xs leading-7 text-white shadow hover:bg-blue-700"
        [style.left.px]="left()"
        [style.width.px]="width()"
        [attr.aria-label]="accessibleLabel()"
        (click)="activated.emit()">
        <span class="whitespace-nowrap">{{ label() }}</span>
      </button>
    }
  `,
})
export class TimelineBarComponent {
  readonly label = input.required<string>();
  readonly accessibleLabel = input.required<string>();
  readonly startDate = input<string | null>();
  readonly endDate = input<string | null>();
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly dayWidth = input.required<number>();
  readonly activated = output();

  readonly milestone = computed(() => !this.startDate() && !!this.endDate());
  readonly left = computed(() => {
    const start = this.startDate() ?? this.endDate() ?? this.from();
    return clippedRangeLeft(this.from(), start, this.dayWidth());
  });
  readonly width = computed(() => {
    const start = this.startDate() ?? this.endDate() ?? this.from();
    const end = this.endDate() ?? this.startDate() ?? start;
    return clippedRangeWidth(
      this.from(),
      this.to(),
      start,
      end,
      this.dayWidth()
    );
  });
}
