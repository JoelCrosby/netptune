import { Directive, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

// A borderless text input set as a page-sized heading, for a title typed in place.
// The negative margin keeps the text aligned with the content below while the
// hover and focus wash extends past it.
@Directive({
  selector: 'input[appHeadingInput]',
  host: { '[class]': 'hostClass()' },
})
export class HeadingInputDirective {
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'placeholder:text-muted -mx-2 w-full rounded bg-transparent px-2 py-1 text-[28px]/[36px] font-semibold tracking-[-0.012em] transition-colors outline-none hover:bg-black/5 focus:bg-black/5 dark:hover:bg-white/5 dark:focus:bg-white/5',
      this.class()
    );
  });
}
