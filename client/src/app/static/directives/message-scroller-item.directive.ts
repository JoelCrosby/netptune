import {
  DestroyRef,
  Directive,
  ElementRef,
  afterNextRender,
  inject,
  input,
} from '@angular/core';
import { MessageScrollerDirective } from './message-scroller.directive';

@Directive({
  selector: '[appMessageScrollerItem]',
  host: { '[attr.data-message-id]': 'messageId()' },
})
export class MessageScrollerItemDirective {
  readonly messageId = input.required<string>({
    alias: 'appMessageScrollerItem',
  });

  readonly scrollAnchor = input(false);

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly scroller = inject(MessageScrollerDirective);
  private readonly destroyRef = inject(DestroyRef);

  private observer?: IntersectionObserver;

  constructor() {
    afterNextRender(() => {
      const element = this.elementRef.nativeElement;

      this.scroller.register(this.messageId(), element, this.scrollAnchor());
      this.observeVisibility(element);
    });

    this.destroyRef.onDestroy(() => {
      this.observer?.disconnect();
      this.scroller.unregister(this.messageId());
    });
  }

  private observeVisibility(element: HTMLElement) {
    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          this.scroller.setVisible(this.messageId(), entry.isIntersecting);
        }
      },
      { root: element.closest('[data-scroll-viewport]') }
    );

    this.observer.observe(element);
  }
}
