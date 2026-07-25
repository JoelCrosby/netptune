import { Component, computed, input } from '@angular/core';
import { SkeletonComponent } from './skeleton.component';

const barOffsets = [4, 18, 32, 12, 46, 24, 8, 38];
const barWidths = [34, 22, 40, 18, 28, 44, 30, 20];

@Component({
  selector: 'app-skeleton-timeline',
  imports: [SkeletonComponent],
  host: {
    class: 'block min-h-0 flex-1 overflow-hidden',
    role: 'status',
    'aria-label': 'Loading timeline',
  },
  template: `
    <div class="bg-card h-full overflow-hidden">
      <div class="border-border flex border-b" [style.height.px]="headerHeight">
        <div
          class="border-border flex shrink-0 items-end border-r px-3 pb-3"
          [style.width.px]="itemColumnWidth">
          <app-skeleton class="h-3 w-16" />
        </div>
        <div class="flex flex-1 items-end gap-10 px-3 pb-3">
          @for (tick of tickRange; track $index) {
            <app-skeleton class="h-3 w-12" />
          }
        </div>
      </div>

      @for (group of groupRange(); track $index; let groupIndex = $index) {
        <div class="border-border bg-muted/30 flex h-9 items-center border-b">
          <div class="shrink-0 px-3" [style.width.px]="itemColumnWidth">
            <app-skeleton class="h-3 w-32" />
          </div>
        </div>

        @for (row of rowRange(); track $index; let rowIndex = $index) {
          <div class="border-border flex h-11 items-center border-b">
            <div class="shrink-0 px-3" [style.width.px]="itemColumnWidth">
              <app-skeleton
                class="h-3"
                [style.width.%]="labelWidth(groupIndex, rowIndex)" />
            </div>
            <div class="flex-1 px-3">
              <app-skeleton
                class="h-5 rounded-full"
                [style.margin-left.%]="barOffset(groupIndex, rowIndex)"
                [style.width.%]="barWidth(groupIndex, rowIndex)" />
            </div>
          </div>
        }
      }
    </div>
  `,
})
export class SkeletonTimelineComponent {
  readonly groups = input(2);
  readonly rowsPerGroup = input(4);

  readonly headerHeight = 80;
  readonly itemColumnWidth = 320;
  readonly tickRange = Array.from({ length: 6 });

  readonly groupRange = computed(() => Array.from({ length: this.groups() }));
  readonly rowRange = computed(() =>
    Array.from({ length: this.rowsPerGroup() })
  );

  barOffset(groupIndex: number, rowIndex: number): number {
    return barOffsets[this.barIndex(groupIndex, rowIndex)];
  }

  barWidth(groupIndex: number, rowIndex: number): number {
    return barWidths[this.barIndex(groupIndex, rowIndex)];
  }

  labelWidth(groupIndex: number, rowIndex: number): number {
    return 45 + barOffsets[this.barIndex(groupIndex, rowIndex)];
  }

  private barIndex(groupIndex: number, rowIndex: number): number {
    return (groupIndex * this.rowsPerGroup() + rowIndex) % barOffsets.length;
  }
}
