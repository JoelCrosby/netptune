import { LiveAnnouncer } from '@angular/cdk/a11y';
import { DOCUMENT } from '@angular/common';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  DestroyRef,
  ElementRef,
  Injector,
  TemplateRef,
  ViewContainerRef,
  afterNextRender,
  assertInInjectionContext,
  inject,
} from '@angular/core';

export interface AnchoredPopupOptions {
  timeout?: number;
  onTimeout?: () => void;
}

export interface AnchoredPopupRef {
  show(
    anchor: ElementRef<HTMLElement> | HTMLElement,
    template: TemplateRef<unknown>
  ): void;
  hide(): void;
}

export function anchoredPopup(
  options: AnchoredPopupOptions = {}
): AnchoredPopupRef {
  assertInInjectionContext(anchoredPopup);

  const overlay = inject(Overlay);
  const viewContainer = inject(ViewContainerRef);
  const liveAnnouncer = inject(LiveAnnouncer);
  const document = inject(DOCUMENT);
  const injector = inject(Injector);

  let overlayRef: OverlayRef | null = null;
  let timer: ReturnType<typeof setTimeout> | null = null;

  const clearTimer = () => {
    if (timer === null) return;

    clearTimeout(timer);
    timer = null;
  };

  const hide = () => {
    clearTimer();
    document.removeEventListener('keydown', onKeydown);
    overlayRef?.dispose();
    overlayRef = null;
  };

  const onKeydown = (event: KeyboardEvent) => {
    if (event.key !== 'Escape') return;

    options.onTimeout?.();
    hide();
  };

  const announce = (pane: HTMLElement) => {
    const text = pane.textContent?.replace(/\s+/g, ' ').trim();

    if (text) void liveAnnouncer.announce(text, 'polite');
  };

  const startTimer = () => {
    clearTimer();

    const timeout = options.timeout;

    if (!timeout) return;

    timer = setTimeout(() => {
      options.onTimeout?.();
      hide();
    }, timeout);
  };

  const show = (
    anchor: ElementRef<HTMLElement> | HTMLElement,
    template: TemplateRef<unknown>
  ) => {
    if (overlayRef) {
      startTimer();

      return;
    }

    const positionStrategy = overlay
      .position()
      .flexibleConnectedTo(anchor)
      .withPush(true)
      .withPositions([
        {
          originX: 'end',
          originY: 'bottom',
          overlayX: 'end',
          overlayY: 'top',
          offsetY: 8,
        },
        {
          originX: 'end',
          originY: 'top',
          overlayX: 'end',
          overlayY: 'bottom',
          offsetY: -8,
        },
      ]);

    overlayRef = overlay.create({
      positionStrategy,
      scrollStrategy: overlay.scrollStrategies.reposition(),
    });

    overlayRef.attach(new TemplatePortal(template, viewContainer));

    const pane = overlayRef.overlayElement;
    pane.addEventListener('mouseenter', clearTimer);
    pane.addEventListener('focusin', clearTimer);
    pane.addEventListener('mouseleave', startTimer);
    pane.addEventListener('focusout', startTimer);
    document.addEventListener('keydown', onKeydown);

    afterNextRender(() => announce(pane), { injector });

    startTimer();
  };

  inject(DestroyRef).onDestroy(hide);

  return { show, hide };
}
