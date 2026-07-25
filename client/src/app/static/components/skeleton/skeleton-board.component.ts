import { Component, computed, input } from '@angular/core';
import { SkeletonComponent } from './skeleton.component';

@Component({
  selector: 'app-skeleton-board',
  imports: [SkeletonComponent],
  host: { class: 'contents', role: 'status', 'aria-label': 'Loading board' },
  template: `
    <div
      class="flex max-h-[calc(100vh-180px)] w-full flex-1 flex-row overflow-hidden rounded-lg pb-4">
      @for (column of columnRange(); track $index) {
        <div
          class="mr-4 flex w-75 flex-none flex-col overflow-hidden rounded-[.4rem]">
          <div
            class="border-border bg-board-group relative flex h-full flex-1 flex-col gap-3 rounded border p-3">
            <div class="flex items-center justify-between">
              <app-skeleton class="h-4 w-24" />
              <app-skeleton class="h-4 w-6" />
            </div>

            @for (card of cardRange(); track $index) {
              <div
                class="border-border bg-background flex flex-col gap-2 rounded-md border p-3">
                <app-skeleton class="h-3 w-5/6" />
                <app-skeleton class="h-3 w-1/2" />
                <app-skeleton class="mt-1 h-5 w-5 rounded-full" />
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class SkeletonBoardComponent {
  readonly columns = input(4);
  readonly cardsPerColumn = input(3);

  readonly columnRange = computed(() => Array.from({ length: this.columns() }));
  readonly cardRange = computed(() =>
    Array.from({ length: this.cardsPerColumn() })
  );
}
