import {
  DestroyRef,
  Directive,
  ElementRef,
  afterNextRender,
  inject,
  signal,
} from '@angular/core';

@Directive({
  selector: '[appScrollOverflow]',
  exportAs: 'appScrollOverflow',
})
export class ScrollOverflowDirective {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly overflowing = signal(false);

  constructor() {
    afterNextRender(() => this.observe());
  }

  private observe() {
    const element = this.element.nativeElement;
    const resizeObserver = new ResizeObserver(() => this.check());

    resizeObserver.observe(element);

    for (const child of Array.from(element.children)) {
      resizeObserver.observe(child);
    }

    const mutationObserver = new MutationObserver((mutations) => {
      for (const mutation of mutations) {
        for (const node of Array.from(mutation.addedNodes)) {
          if (node instanceof Element) resizeObserver.observe(node);
        }

        for (const node of Array.from(mutation.removedNodes)) {
          if (node instanceof Element) resizeObserver.unobserve(node);
        }
      }

      this.check();
    });

    mutationObserver.observe(element, { childList: true });

    this.destroyRef.onDestroy(() => {
      resizeObserver.disconnect();
      mutationObserver.disconnect();
    });
  }

  // Both heights are rounded to whole pixels, so content that exactly fills the
  // element can read 1px taller at fractional zoom levels.
  private check() {
    const element = this.element.nativeElement;

    this.overflowing.set(element.scrollHeight > element.clientHeight + 1);
  }
}
