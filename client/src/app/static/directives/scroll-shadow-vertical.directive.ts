import { AfterViewInit, Directive, ElementRef, OnDestroy } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

const TOP_CLASS = 'scroll-shadow-top';
const BOTTOM_CLASS = 'scroll-shadow-bottom';

@Directive({ selector: '[appScrollShadowVertical]' })
export class ScrollShadowVericalDirective implements AfterViewInit, OnDestroy {
  topShadowEl!: Element;
  bottomShadowEl!: Element;

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

      this.topShadowEl = this.setScrollShadowTop();
      this.bottomShadowEl = this.setScrollShadowBottom();

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

  private getShadowDiv(className: string) {
    const shadowDiv = document.createElement('div');

    shadowDiv.setAttribute(`data-scroll-shadow`, className);

    return shadowDiv;
  }

  private setScrollShadowTop() {
    const div = this.getShadowDiv(TOP_CLASS);
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  private setScrollShadowBottom() {
    const div = this.getShadowDiv(BOTTOM_CLASS);
    this.element.insertAdjacentElement('afterbegin', div);

    return div;
  }

  private toggleShadowElClass(el: 'top' | 'bottom', value: boolean) {
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
