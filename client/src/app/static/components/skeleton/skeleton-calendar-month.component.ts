import { Component, computed, input } from '@angular/core';
import { SkeletonComponent } from './skeleton.component';

const chipCounts = [2, 0, 1, 3, 1, 0, 2];

@Component({
  selector: 'app-skeleton-calendar-month',
  imports: [SkeletonComponent],
  host: {
    class: 'block min-h-0 flex-1 overflow-hidden',
    role: 'status',
    'aria-label': 'Loading calendar',
  },
  template: `
    <div class="bg-card grid grid-cols-7">
      @for (weekday of weekdayRange; track $index) {
        <div
          class="border-border bg-muted/10 flex justify-center border-r border-b px-2 py-1.5 last:border-r-0">
          <app-skeleton class="h-3 w-8" />
        </div>
      }
    </div>

    @for (week of weekRange(); track $index; let weekIndex = $index) {
      <div class="grid grid-cols-7">
        @for (day of weekdayRange; track $index; let dayIndex = $index) {
          <div
            class="border-border min-h-28 min-w-0 border-r border-b p-1 last:border-r-0 sm:min-h-32 sm:p-1.5">
            <div class="mb-2 flex items-center">
              <app-skeleton class="h-6 w-6 rounded-full" />
            </div>

            <div class="space-y-1 pb-1">
              @for (chip of chipRange(weekIndex, dayIndex); track $index) {
                <app-skeleton class="h-5 w-full" />
              }
            </div>
          </div>
        }
      </div>
    }
  `,
})
export class SkeletonCalendarMonthComponent {
  readonly weeks = input(5);

  readonly weekdayRange = Array.from({ length: 7 });

  readonly weekRange = computed(() => Array.from({ length: this.weeks() }));

  chipRange(weekIndex: number, dayIndex: number) {
    const count = chipCounts[(weekIndex * 3 + dayIndex) % chipCounts.length];

    return Array.from({ length: count });
  }
}
