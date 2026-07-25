import { Component, computed, input } from '@angular/core';
import { SkeletonComponent } from './skeleton.component';

@Component({
  selector: 'app-skeleton-card-grid',
  imports: [SkeletonComponent],
  host: { class: 'block', role: 'status', 'aria-label': 'Loading' },
  template: `
    <div
      class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      @for (card of cardRange(); track $index) {
        <div
          class="border-border bg-card flex flex-col gap-3 rounded-xl border p-4">
          <app-skeleton class="h-5 w-2/3" />
          <app-skeleton class="h-3 w-full" />
          <app-skeleton class="h-3 w-4/5" />

          <div class="mt-2 flex items-center gap-2">
            <app-skeleton class="h-6 w-6 rounded-full" />
            <app-skeleton class="h-3 w-20" />
          </div>
        </div>
      }
    </div>
  `,
})
export class SkeletonCardGridComponent {
  readonly cards = input(6);

  readonly cardRange = computed(() => Array.from({ length: this.cards() }));
}
