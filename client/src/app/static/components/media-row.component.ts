import { Component, computed, input } from '@angular/core';
import { cn } from './button/button.variants';
import { listRowVariants } from './list-row.component';

// A bordered row led by a picture or preview, with a heading, a description and
// trailing actions that wrap underneath on narrow screens.
@Component({
  selector: 'app-media-row',
  host: { '[class]': 'hostClass()' },
  template: `
    <div class="shrink-0">
      <ng-content select="[mediaRowMedia]" />
    </div>

    <div class="flex min-w-48 flex-1 flex-col gap-1.5">
      <p class="text-sm font-semibold">{{ heading() }}</p>
      <div class="text-muted text-sm leading-snug">
        <ng-content />
      </div>
    </div>

    <div class="flex shrink-0 items-center gap-2 empty:hidden">
      <ng-content select="[mediaRowActions]" />
    </div>

    <ng-content select="[mediaRowOverlay]" />
  `,
})
export class MediaRowComponent {
  readonly heading = input.required<string>();
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      listRowVariants({ tone: 'neutral' }),
      'relative flex-wrap gap-x-5 gap-y-4 p-4',
      this.class()
    );
  });
}
