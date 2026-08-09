import { Component, input, output, signal } from '@angular/core';
import {
  MAX_AI_PANEL_WIDTH,
  MIN_AI_PANEL_WIDTH,
} from '@core/models/ai-panel-width';

interface ResizeOrigin {
  clientX: number;
  width: number;
}

const KEYBOARD_STEP = 24;

@Component({
  selector: 'app-ai-assistant-resize-handle',
  host: { class: 'contents' },
  template: `
    <div
      role="separator"
      tabindex="0"
      aria-orientation="vertical"
      class="group absolute inset-y-0 left-0 z-20 flex w-2 cursor-ew-resize touch-none items-center justify-center focus-visible:outline-none"
      i18n-aria-label="
        Accessible name of the handle that resizes the assistant panel
      "
      aria-label="Resize assistant panel"
      [attr.aria-valuenow]="width()"
      [attr.aria-valuemin]="minWidth"
      [attr.aria-valuemax]="maxWidth"
      (pointerdown)="startResize($event)"
      (pointermove)="trackResize($event)"
      (pointerup)="endResize($event)"
      (pointercancel)="cancelResize($event)"
      (keydown)="adjustWidth($event)">
      <span
        class="bg-primary h-10 w-0.5 rounded-full opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100"
        [class.opacity-100]="resizing()"></span>
    </div>
  `,
})
export class AiAssistantResizeHandleComponent {
  readonly width = input.required<number>();
  readonly widthChange = output<number>();
  readonly resizingChange = output<boolean>();

  protected readonly minWidth = MIN_AI_PANEL_WIDTH;
  protected readonly maxWidth = MAX_AI_PANEL_WIDTH;

  protected readonly resizing = signal(false);

  private origin: ResizeOrigin | null = null;

  protected startResize(event: PointerEvent) {
    if (event.button !== 0) {
      return;
    }

    event.preventDefault();

    const handle = event.currentTarget as HTMLElement;

    handle.setPointerCapture(event.pointerId);

    this.origin = { clientX: event.clientX, width: this.width() };
    this.resizing.set(true);
    this.resizingChange.emit(true);
  }

  protected trackResize(event: PointerEvent) {
    const origin = this.origin;

    if (origin === null) {
      return;
    }

    this.widthChange.emit(origin.width + origin.clientX - event.clientX);
  }

  protected endResize(event: PointerEvent) {
    if (this.origin === null) {
      return;
    }

    this.trackResize(event);
    this.releaseResize(event);
  }

  protected cancelResize(event: PointerEvent) {
    const origin = this.origin;

    if (origin === null) {
      return;
    }

    this.widthChange.emit(origin.width);
    this.releaseResize(event);
  }

  private releaseResize(event: PointerEvent) {
    const handle = event.currentTarget as HTMLElement;
    const hasCapture = handle.hasPointerCapture(event.pointerId);

    if (hasCapture) {
      handle.releasePointerCapture(event.pointerId);
    }

    this.origin = null;
    this.resizing.set(false);
    this.resizingChange.emit(false);
  }

  protected adjustWidth(event: KeyboardEvent) {
    const step = keyboardStep(event.key);

    if (step === 0) {
      return;
    }

    event.preventDefault();
    this.widthChange.emit(this.width() + step);
  }
}

function keyboardStep(key: string): number {
  if (key === 'ArrowLeft') {
    return KEYBOARD_STEP;
  }

  if (key === 'ArrowRight') {
    return -KEYBOARD_STEP;
  }

  return 0;
}
