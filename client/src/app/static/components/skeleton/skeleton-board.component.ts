import { Component, computed, input } from '@angular/core';
import { SkeletonComponent } from './skeleton.component';

@Component({
  selector: 'app-skeleton-board',
  imports: [SkeletonComponent],
  host: { class: 'contents', role: 'status', 'aria-label': 'Loading board' },
  template: `
    <div
      class="flex min-h-0 w-full flex-1 flex-row overflow-hidden rounded-lg pb-4">
      @for (column of columnRange(); track $index) {
        <div
          class="mr-4 flex w-[80vw] flex-none flex-col overflow-hidden rounded-[.4rem] md:w-75">
          <div
            class="border-border bg-board-group relative flex h-full flex-1 flex-col rounded border">
            <div class="flex h-12.5 shrink-0 items-center gap-3 px-4">
              <app-skeleton class="h-3.5 w-4" />
              <app-skeleton class="h-3.5 w-24" />
            </div>

            <div class="flex flex-col overflow-hidden p-[.6rem]">
              @for (card of cardRange(); track $index) {
                <div
                  class="border-border bg-board-group-card mb-[.3rem] flex min-h-24 flex-col rounded-sm border p-2 shadow-sm">
                  <app-skeleton class="mt-1 h-3.5 w-5/6" />
                  <app-skeleton class="mt-2 h-3.5 w-1/2" />

                  <div class="mt-4 flex flex-row gap-[.4rem]">
                    <app-skeleton class="h-6 w-20 rounded-[4px]" />
                    <app-skeleton class="h-6 w-24 rounded-[4px]" />
                  </div>

                  <div
                    class="mt-3 flex w-full flex-row items-center justify-between">
                    <app-skeleton class="h-5 w-16" />
                    <app-skeleton class="h-6 w-6 rounded-full" />
                  </div>
                </div>
              }
            </div>
          </div>
        </div>
      }
    </div>
  `,
})
export class SkeletonBoardComponent {
  readonly columns = input(5);
  readonly cardsPerColumn = input(4);

  readonly columnRange = computed(() => Array.from({ length: this.columns() }));
  readonly cardRange = computed(() =>
    Array.from({ length: this.cardsPerColumn() })
  );
}
