import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { ConnectedPosition } from '@angular/cdk/overlay';
import { Directive, ElementRef, inject, input, OnDestroy } from '@angular/core';
import { TooltipComponent } from '../components/tooltip/tooltip.component';

export type TooltipPosition = 'top' | 'bottom' | 'left' | 'right';

const POSITIONS: Record<TooltipPosition, ConnectedPosition> = {
  top: {
    originX: 'center',
    originY: 'top',
    overlayX: 'center',
    overlayY: 'bottom',
    offsetY: -8,
  },
  bottom: {
    originX: 'center',
    originY: 'bottom',
    overlayX: 'center',
    overlayY: 'top',
    offsetY: 8,
  },
  left: {
    originX: 'start',
    originY: 'center',
    overlayX: 'end',
    overlayY: 'center',
    offsetX: -8,
  },
  right: {
    originX: 'end',
    originY: 'center',
    overlayX: 'start',
    overlayY: 'center',
    offsetX: 8,
  },
};

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
  appTooltip = input<string | null | undefined>();
  appTooltipPosition = input<TooltipPosition>('top');
  appTooltipDisabled = input<boolean>(false);

  private el = inject(ElementRef);
  private overlay = inject(Overlay);

  private overlayRef: OverlayRef | null = null;

  show() {
    const text = this.appTooltip();
    if (!text || this.appTooltipDisabled() || this.overlayRef) return;

    const position = this.appTooltipPosition();
    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(this.el)
      .withPositions([POSITIONS[position]])
      .withPush(true);

    this.overlayRef = this.overlay.create({
      positionStrategy,
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
    });

    const ref = this.overlayRef.attach(new ComponentPortal(TooltipComponent));
    ref.setInput('text', text);
    ref.setInput('position', position);
  }

  hide() {
    if (!this.overlayRef) return;
    this.overlayRef.dispose();
    this.overlayRef = null;
  }

  ngOnDestroy() {
    this.hide();
  }
}
