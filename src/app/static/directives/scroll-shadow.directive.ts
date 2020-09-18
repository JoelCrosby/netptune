import { AfterViewInit, Directive, ElementRef, OnDestroy } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

const LEFT_CLASS = 'scroll-shadow-left';
const RIGHT_CLASS = 'scroll-shadow-right';

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
    setTimeout(() => {
      if (!this.element) return;

      fromEvent(this.element, 'scroll', {
        passive: true,
      })
        .pipe(takeUntil(this.onDestroy$))
        .subscribe({ next: () => this._checkScroll() });

      this.leftShadowEl = this._setScrollShadowLeft();
      this.rightShadowEl = this._setScrollShadowRight();

      this._checkScroll();
    }, 1000);
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
      this._toggleShadowElClass('left', true);
    } else {
      this._toggleShadowElClass('left', false);
    }

    if (
      this.element.scrollLeft + this.element.clientWidth <
      this.element.scrollWidth
    ) {
      this._toggleShadowElClass('right', true);
    } else {
      this._toggleShadowElClass('right', false);
    }
  }

  private _getShadowDiv(className: string) {
    const shadowDiv = document.createElement('div');

    shadowDiv.setAttribute(`data-scroll-shadow`, className);

    return shadowDiv;
  }

  private _setScrollShadowLeft() {
    const div = this._getShadowDiv(LEFT_CLASS);
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  private _setScrollShadowRight() {
    const div = this._getShadowDiv(RIGHT_CLASS);
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  private _toggleShadowElClass(el: 'left' | 'right', value: boolean) {
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
