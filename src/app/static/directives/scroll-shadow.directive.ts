import { AfterViewInit, Directive, ElementRef, OnDestroy } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { takeUntil, throttleTime, observeOn } from 'rxjs/operators';
import { AnimationFrameScheduler } from 'rxjs/internal/scheduler/AnimationFrameScheduler';

@Directive({
  selector: '[appScrollShadow]',
})
export class ScrollShadowDirective implements AfterViewInit, OnDestroy {
  leftShadowEl: Element;
  rightShadowEl: Element;

  onDestroy$ = new Subject();

  get element() {
    return this.elementRef.nativeElement;
  }

  constructor(private elementRef: ElementRef) {}

  ngAfterViewInit() {
    if (!this.element) return;

    fromEvent(this.element, 'scroll', {
      passive: true,
    })
      .pipe(takeUntil(this.onDestroy$))
      .subscribe({ next: () => this._checkScroll() });

    this.leftShadowEl = this.setScrollShadowLeft();
    this.rightShadowEl = this.setScrollShadowRight();

    this._checkScroll();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  private _checkScroll() {
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

  getShadowDiv(className: string) {
    const shadowDiv = document.createElement('div');

    shadowDiv.setAttribute(`data-scroll-shadow`, className);

    return shadowDiv;
  }

  setScrollShadowLeft() {
    const div = this.getShadowDiv('scroll-shadow-left');
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  setScrollShadowRight() {
    const div = this.getShadowDiv('scroll-shadow-right');
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  toggleShadowElClass(el: 'left' | 'right', value: boolean) {
    const toggleEl = (target: Element) => {
      if (value) {
        target.setAttribute('class', `scroll-shadow-${el}`);
      } else {
        target.setAttribute('class', '');
      }
    };

    if (el === 'left') {
      toggleEl(this.leftShadowEl);
    } else {
      toggleEl(this.rightShadowEl);
    }
  }
}
