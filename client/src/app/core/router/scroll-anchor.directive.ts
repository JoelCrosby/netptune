import { Directive, effect, ElementRef, inject, input } from '@angular/core';
import { ScrollAnchorService } from '@core/router/scroll-anchor.service';

@Directive({
  selector: '[appScrollAnchor]',
  host: {
    '[attr.id]': 'anchorId()',
    tabindex: '-1',
    class: 'scroll-mt-[var(--scroll-anchor-offset,0px)] outline-none',
  },
})
export class ScrollAnchorDirective {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly anchors = inject(ScrollAnchorService);

  readonly anchorId = input.required<string>({ alias: 'appScrollAnchor' });

  constructor() {
    effect((onCleanup) => {
      const id = this.anchorId();
      const element = this.element.nativeElement;

      this.anchors.register(id, element);

      onCleanup(() => this.anchors.unregister(id, element));
    });
  }
}
