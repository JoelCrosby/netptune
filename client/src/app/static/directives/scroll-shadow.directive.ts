import { AfterViewInit, Directive, ElementRef, OnDestroy } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

const LEFT_CLASS = 'scroll-shadow-left';
const RIGHT_CLASS = 'scroll-shadow-right';

@Directive({ selector: '[appScrollShadow]' })
export class ScrollShadowDirective implements AfterViewInit, OnDestroy {
  leftShadowEl!: Element;
  rightShadowEl!: Element;

  onDestroy$ = new Subject<void>();

  get element() {
    return this.elementRef.nativeElement as Element;
  }

  constructor(private elementRef: ElementRef) {}

  ngAfterViewInit() {
    setTimeout(() => {
      if (!this.element) return;

      fromEvent(this.element, 'scroll', {
        passive: true,
      })
        .pipe(takeUntil(this.onDestroy$))
        .subscribe({ next: () => this.checkScroll() });

      this.leftShadowEl = this.setScrollShadowLeft();
      this.rightShadowEl = this.setScrollShadowRight();

      this.checkScroll();
    }, 1000);
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  private checkScroll() {
    if (!this.element) {
      return;
    }

    if (this.element.scrollLeft > 8) {
      this.toggleShadowElClass('left', true);
    } else {
      this.toggleShadowElClass('left', false);
    }

    if (
      this.element.scrollLeft + this.element.clientWidth <
      this.element.scrollWidth
    ) {
      this.toggleShadowElClass('right', true);
    } else {
      this.toggleShadowElClass('right', false);
    }
  }

  private getShadowDiv(className: string) {
    const shadowDiv = document.createElement('div');

    shadowDiv.setAttribute(`data-scroll-shadow`, className);

    return shadowDiv;
  }

  private setScrollShadowLeft() {
    const div = this.getShadowDiv(LEFT_CLASS);
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  private setScrollShadowRight() {
    const div = this.getShadowDiv(RIGHT_CLASS);
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  private toggleShadowElClass(el: 'left' | 'right', value: boolean) {
    const toggleEl = (target: Element, cls: string) => {
      if (value) {
        target.setAttribute('class', cls);
      } else {
        target.setAttribute('class', '');
      }
    };

    if (el === 'left') {
      toggleEl(this.leftShadowEl, LEFT_CLASS);
    } else {
      toggleEl(this.rightShadowEl, RIGHT_CLASS);
    }
  }
}
