import {
  ApplicationRef,
  ComponentRef,
  createComponent,
  Directive,
  ElementRef,
  EnvironmentInjector,
  inject,
  input,
  OnDestroy,
  Renderer2,
} from '@angular/core';
import { TooltipComponent } from '../components/tooltip/tooltip.component';

export type TooltipPosition = 'top' | 'bottom' | 'left' | 'right';

@Directive({
  selector: '[appTooltip]',
  host: {
    '(mouseenter)': 'show()',
    '(mouseleave)': 'hide()',
    '(focusin)': 'show()',
    '(focusout)': 'hide()',
  },
})
export class TooltipDirective implements OnDestroy {
  appTooltip = input<string>('');
  appTooltipPosition = input<TooltipPosition>('top');
  appTooltipDisabled = input<boolean>(false);

  private el = inject(ElementRef);
  private renderer = inject(Renderer2);
  private appRef = inject(ApplicationRef);
  private injector = inject(EnvironmentInjector);

  private tooltipRef: ComponentRef<TooltipComponent> | null = null;

  show() {
    const text = this.appTooltip();
    if (!text || this.appTooltipDisabled() || this.tooltipRef) return;

    this.tooltipRef = createComponent(TooltipComponent, {
      environmentInjector: this.injector,
    });

    this.tooltipRef.setInput('text', text);
    this.tooltipRef.setInput('position', this.appTooltipPosition());

    this.appRef.attachView(this.tooltipRef.hostView);
    this.renderer.appendChild(
      document.body,
      this.tooltipRef.location.nativeElement,
    );

    this.tooltipRef.changeDetectorRef.detectChanges();

    this.position();
  }

  hide() {
    if (!this.tooltipRef) return;
    this.appRef.detachView(this.tooltipRef.hostView);
    this.tooltipRef.destroy();
    this.tooltipRef = null;
  }

  ngOnDestroy() {
    this.hide();
  }

  private position() {
    if (!this.tooltipRef) return;

    const tooltipEl: HTMLElement =
      this.tooltipRef.location.nativeElement.firstElementChild;
    if (!tooltipEl) return;

    const hostRect: DOMRect = this.el.nativeElement.getBoundingClientRect();
    const tooltipRect = tooltipEl.getBoundingClientRect();
    const gap = 8;

    let top = 0;
    let left = 0;

    switch (this.appTooltipPosition()) {
      case 'top':
        top = hostRect.top - tooltipRect.height - gap;
        left = hostRect.left + (hostRect.width - tooltipRect.width) / 2;
        break;
      case 'bottom':
        top = hostRect.bottom + gap;
        left = hostRect.left + (hostRect.width - tooltipRect.width) / 2;
        break;
      case 'left':
        top = hostRect.top + (hostRect.height - tooltipRect.height) / 2;
        left = hostRect.left - tooltipRect.width - gap;
        break;
      case 'right':
        top = hostRect.top + (hostRect.height - tooltipRect.height) / 2;
        left = hostRect.right + gap;
        break;
    }

    // Keep within viewport
    left = Math.max(8, Math.min(left, window.innerWidth - tooltipRect.width - 8));
    top = Math.max(8, Math.min(top, window.innerHeight - tooltipRect.height - 8));

    tooltipEl.style.top = `${top + window.scrollY}px`;
    tooltipEl.style.left = `${left + window.scrollX}px`;
  }
}
