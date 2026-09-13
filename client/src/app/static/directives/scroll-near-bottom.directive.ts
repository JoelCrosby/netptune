import { Directive, ElementRef, inject, output } from '@angular/core';

const nearBottomThresholdPx = 48;

@Directive({
  selector: '[appScrollNearBottom]',
  host: { '(scroll)': 'onScroll()' },
})
export class ScrollNearBottomDirective {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly nearBottom = output();

  protected onScroll() {
    const element = this.element.nativeElement;
    const distanceToBottom =
      element.scrollHeight - element.scrollTop - element.clientHeight;

    if (distanceToBottom > nearBottomThresholdPx) return;

    this.nearBottom.emit();
  }
}
