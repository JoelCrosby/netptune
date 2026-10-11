import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-hero-panel',
  host: { '[class]': 'hostClass()' },
  template: `
    <section
      class="border-primary/20 bg-primary/5 flex flex-col items-center gap-[18px] rounded-[14px] border px-8 pt-10 pb-[30px] max-md:px-4 max-md:pt-6 max-md:pb-5">
      <div class="flex flex-col items-center gap-1.5 text-center">
        <h2 class="text-[26px] font-semibold tracking-[-0.01em] max-md:text-xl">
          {{ heading() }}
        </h2>
        @if (description()) {
          <p class="text-foreground/65 text-sm">{{ description() }}</p>
        }
      </div>

      <ng-content />
    </section>
  `,
})
export class HeroPanelComponent {
  readonly heading = input.required<string>();
  readonly description = input('');
  readonly class = input('');

  protected readonly hostClass = computed(() => cn('block', this.class()));
}
