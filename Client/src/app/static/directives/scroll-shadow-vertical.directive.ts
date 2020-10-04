import { AfterViewInit, Directive, ElementRef, OnDestroy } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

const TOP_CLASS = 'scroll-shadow-top';
const BOTTOM_CLASS = 'scroll-shadow-bottom';

@Directive({
  selector: '[appScrollShadowVertical]',
})
export class ScrollShadowVericalDirective implements AfterViewInit, OnDestroy {
  topShadowEl: Element;
  bottomShadowEl: Element;

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

      this.topShadowEl = this._setScrollShadowTop();
      this.bottomShadowEl = this._setScrollShadowBottom();

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

    if (this.element.scrollTop > 8) {
      this._toggleShadowElClass('top', true);
    } else {
      this._toggleShadowElClass('top', false);
    }

    if (
      this.element.scrollTop + this.element.clientHeight <
      this.element.scrollHeight
    ) {
      this._toggleShadowElClass('bottom', true);
    } else {
      this._toggleShadowElClass('bottom', false);
    }
  }

  private _getShadowDiv(className: string) {
    const shadowDiv = document.createElement('div');

    shadowDiv.setAttribute(`data-scroll-shadow`, className);

    return shadowDiv;
  }

  private _setScrollShadowTop() {
    const div = this._getShadowDiv(TOP_CLASS);
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  private _setScrollShadowBottom() {
    const div = this._getShadowDiv(BOTTOM_CLASS);
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  private _toggleShadowElClass(el: 'top' | 'bottom', value: boolean) {
    const toggleEl = (target: Element, cls: string) => {
      if (value) {
        target.setAttribute('class', cls);
      } else {
        target.setAttribute('class', '');
      }
    };

    if (el === 'top') {
      toggleEl(this.topShadowEl, TOP_CLASS);
    } else {
      toggleEl(this.bottomShadowEl, BOTTOM_CLASS);
    }
  }
}
