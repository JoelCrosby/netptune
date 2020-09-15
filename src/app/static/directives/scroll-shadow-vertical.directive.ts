import { AfterViewInit, Directive, ElementRef, OnDestroy } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

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
    if (!this.element) return;

    fromEvent(this.element, 'scroll', {
      passive: true,
    })
      .pipe(takeUntil(this.onDestroy$))
      .subscribe({ next: () => this._checkScroll() });

    this.topShadowEl = this.setScrollShadowTop();
    this.bottomShadowEl = this.setScrollShadowBottom();

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

    console.log({ elementRef: this.elementRef, element: this.element });

    if (this.element.scrollTop > 8) {
      this.toggleShadowElClass('top', true);
    } else {
      this.toggleShadowElClass('top', false);
    }

    if (
      this.element.scrollTop + this.element.clientHeight <
      this.element.scrollHeight
    ) {
      this.toggleShadowElClass('bottom', true);
    } else {
      this.toggleShadowElClass('bottom', false);
    }
  }

  getShadowDiv(className: string) {
    const shadowDiv = document.createElement('div');

    shadowDiv.setAttribute(`data-scroll-shadow`, className);

    return shadowDiv;
  }

  setScrollShadowTop() {
    const div = this.getShadowDiv('scroll-shadow-top');
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  setScrollShadowBottom() {
    const div = this.getShadowDiv('scroll-shadow-bottom');
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  toggleShadowElClass(el: 'top' | 'bottom', value: boolean) {
    const toggleEl = (target: Element) => {
      if (value) {
        target.setAttribute('class', `scroll-shadow-${el}`);
      } else {
        target.setAttribute('class', '');
      }
    };

    if (el === 'top') {
      toggleEl(this.topShadowEl);
    } else {
      toggleEl(this.bottomShadowEl);
    }
  }
}
