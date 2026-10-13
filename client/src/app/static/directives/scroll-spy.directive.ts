import {
  afterNextRender,
  DestroyRef,
  Directive,
  ElementRef,
  inject,
  input,
  output,
} from '@angular/core';

const activationOffsetPx = 96;
const bottomTolerancePx = 2;
const holdSettleMs = 150;

@Directive({
  selector: '[appScrollSpy]',
  host: { '(scroll)': 'onScroll()' },
})
export class ScrollSpyDirective {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly targets = input.required<string>({ alias: 'appScrollSpy' });

  readonly activeIndexChange = output<number | null>();

  private activeIndex: number | null | undefined;
  private frame: number | null = null;
  private holdTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    const resizes = new ResizeObserver(() => this.onResize());

    afterNextRender(() => {
      const container = this.element.nativeElement;

      for (const child of Array.from(container.children)) {
        resizes.observe(child);
      }

      this.update();
    });

    inject(DestroyRef).onDestroy(() => {
      resizes.disconnect();
      this.cancelFrame();
      this.clearHold();
    });
  }

  // Keeps the index until the scroll it starts has settled, because a target
  // that cannot reach the top would otherwise hand the highlight to a later one.
  hold(index: number) {
    this.cancelFrame();
    this.setActive(index);
    this.restartHold();
  }

  protected onScroll() {
    if (this.holdTimer !== null) {
      this.restartHold();

      return;
    }

    this.schedule();
  }

  private onResize() {
    if (this.holdTimer !== null) return;

    this.schedule();
  }

  private schedule() {
    if (this.frame !== null) return;

    this.frame = requestAnimationFrame(() => {
      this.frame = null;
      this.update();
    });
  }

  private restartHold() {
    this.clearHold();
    this.holdTimer = setTimeout(() => {
      this.holdTimer = null;
    }, holdSettleMs);
  }

  private clearHold() {
    if (this.holdTimer === null) return;

    clearTimeout(this.holdTimer);
    this.holdTimer = null;
  }

  private cancelFrame() {
    if (this.frame === null) return;

    cancelAnimationFrame(this.frame);
    this.frame = null;
  }

  private update() {
    this.setActive(this.findActiveIndex());
  }

  private setActive(index: number | null) {
    if (index === this.activeIndex) return;

    this.activeIndex = index;
    this.activeIndexChange.emit(index);
  }

  private queryTargets(): Element[] {
    return Array.from(
      this.element.nativeElement.querySelectorAll(this.targets())
    );
  }

  private findActiveIndex(): number | null {
    const container = this.element.nativeElement;
    const targets = this.queryTargets();

    if (targets.length === 0) return null;

    const bounds = container.getBoundingClientRect();
    const tops = targets.map((target) => {
      return target.getBoundingClientRect().top - bounds.top;
    });

    const distanceToBottom =
      container.scrollHeight - container.scrollTop - container.clientHeight;
    const isAtBottom = distanceToBottom <= bottomTolerancePx;
    const activationLine = isAtBottom
      ? container.clientHeight
      : activationOffsetPx;

    const passed = tops.filter((top) => top <= activationLine).length;

    return passed === 0 ? null : passed - 1;
  }
}
