import { Component, computed, input } from '@angular/core';
import { SkeletonComponent } from './skeleton.component';

@Component({
  selector: 'app-skeleton-card-grid',
  imports: [SkeletonComponent],
  host: {
    class: 'block',
    role: 'status',
    '[attr.aria-label]': 'label()',
  },
  template: `
    <div [class]="gridClass()">
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
  // Defaults to the card grid the boards page uses; a page laying its cards out differently
  // passes its own so the skeleton does not reflow when the content arrives.
  readonly gridClass = input(
    'grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4'
  );
  readonly label = input(
    $localize`:Accessible label while a grid of cards loads:Loading`
  );

  readonly cardRange = computed(() => Array.from({ length: this.cards() }));
}
