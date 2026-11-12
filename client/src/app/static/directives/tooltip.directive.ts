import { AriaDescriber } from '@angular/cdk/a11y';
import { DOCUMENT } from '@angular/common';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { ConnectedPosition } from '@angular/cdk/overlay';
import {
  afterRenderEffect,
  Directive,
  ElementRef,
  inject,
  input,
  OnDestroy,
} from '@angular/core';
import { TooltipComponent } from '../components/tooltip/tooltip.component';

export type TooltipPosition = 'top' | 'bottom' | 'left' | 'right';

const HOVER_HIDE_DELAY_MS = 100;

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
    '(mouseleave)': 'scheduleHide()',
    '(focusin)': 'show()',
    '(focusout)': 'hide()',
  },
})
export class TooltipDirective implements OnDestroy {
  appTooltip = input<string | null | undefined>();
  appTooltipPosition = input<TooltipPosition>('top');
  appTooltipDisabled = input<boolean>(false);

  private el = inject<ElementRef<HTMLElement>>(ElementRef);
  private overlay = inject(Overlay);
  private ariaDescriber = inject(AriaDescriber);
  private document = inject(DOCUMENT);

  private overlayRef: OverlayRef | null = null;
  private hideTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    afterRenderEffect((onCleanup) => {
      const text = this.appTooltip();

      if (!text || this.appTooltipDisabled()) return;

      const element = this.el.nativeElement;

      this.ariaDescriber.describe(element, text, 'tooltip');
      onCleanup(() => {
        this.ariaDescriber.removeDescription(element, text, 'tooltip');
      });
    });
  }

  show() {
    this.clearHideTimer();

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

    const pane = this.overlayRef.overlayElement;
    pane.addEventListener('mouseenter', () => this.clearHideTimer());
    pane.addEventListener('mouseleave', () => this.scheduleHide());

    this.document.addEventListener('keydown', this.onDocumentKeydown);
  }

  hide() {
    this.clearHideTimer();

    if (!this.overlayRef) return;

    this.document.removeEventListener('keydown', this.onDocumentKeydown);
    this.overlayRef.dispose();
    this.overlayRef = null;
  }

  scheduleHide() {
    this.clearHideTimer();
    this.hideTimer = setTimeout(() => this.hide(), HOVER_HIDE_DELAY_MS);
  }

  private readonly onDocumentKeydown = (event: KeyboardEvent) => {
    if (event.key === 'Escape') this.hide();
  };

  private clearHideTimer() {
    if (this.hideTimer === null) return;

    clearTimeout(this.hideTimer);
    this.hideTimer = null;
  }

  ngOnDestroy() {
    this.hide();
  }
}
