import {
  afterNextRender,
  DestroyRef,
  Directive,
  ElementRef,
  inject,
} from '@angular/core';

const ANCHOR_GAP_PX = 16;

@Directive({ selector: '[appStickyOffset]' })
export class StickyOffsetDirective {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

  constructor() {
    const resizes = new ResizeObserver(() => this.publishOffset());

    afterNextRender(() => resizes.observe(this.element.nativeElement));

    inject(DestroyRef).onDestroy(() => resizes.disconnect());
  }

  private publishOffset() {
    const element = this.element.nativeElement;
    const stickyTop = parseFloat(getComputedStyle(element).top) || 0;
    const offset = stickyTop + element.offsetHeight + ANCHOR_GAP_PX;

    element.parentElement?.style.setProperty(
      '--scroll-anchor-offset',
      `${offset}px`
    );
  }
}
