import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  Component,
  HostListener,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  viewChild,
} from '@angular/core';
import { AiPanelService } from '@core/services/ai-panel.service';
import { AiAssistantPanelComponent } from './ai-assistant-panel.component';

const NAVBAR_HEIGHT = '60px';
const PANEL_MARGIN = '1rem';

@Component({
  selector: 'app-ai-assistant',
  imports: [AiAssistantPanelComponent],
  template: `
    <ng-template #panelTmpl>
      <app-ai-assistant-panel
        class="assistant-panel border-border overflow-hidden rounded-2xl border shadow-lg" />
    </ng-template>
  `,
  styles: [
    `
      @keyframes assistant-panel-in {
        from {
          opacity: 0;
          transform: translateX(1rem) scale(0.98);
        }
        to {
          opacity: 1;
          transform: translateX(0) scale(1);
        }
      }

      .assistant-panel {
        animation: assistant-panel-in 180ms ease-out;
        transform-origin: top right;
      }

      @media (prefers-reduced-motion: reduce) {
        .assistant-panel {
          animation: none;
        }
      }
    `,
  ],
})
export class AiAssistantComponent implements OnDestroy {
  private readonly panelTmpl = viewChild<TemplateRef<unknown>>('panelTmpl');

  private readonly overlay = inject(Overlay);
  private readonly vcr = inject(ViewContainerRef);
  private readonly overlayRef: OverlayRef;
  private portal: TemplatePortal | null = null;

  protected readonly panel = inject(AiPanelService);

  constructor() {
    this.overlayRef = this.overlay.create({
      hasBackdrop: false,
      positionStrategy: this.overlay
        .position()
        .global()
        .right(PANEL_MARGIN)
        .top(`calc(${NAVBAR_HEIGHT} + ${PANEL_MARGIN})`),
      scrollStrategy: this.overlay.scrollStrategies.noop(),
      width: this.overlayWidth(),
      height: `calc(100% - ${NAVBAR_HEIGHT} - 2 * ${PANEL_MARGIN})`,
    });

    effect(() => {
      this.overlayRef.updateSize({ width: this.overlayWidth() });
    });

    effect(() => {
      const isOpen = this.panel.isOverlayOpen();
      const template = this.panelTmpl();

      if (!template) {
        return;
      }

      const isAttached = this.overlayRef.hasAttached();

      if (isOpen && !isAttached) {
        this.portal ??= new TemplatePortal(template, this.vcr);
        this.overlayRef.attach(this.portal);

        return;
      }

      if (!isOpen && isAttached) {
        this.overlayRef.detach();
      }
    });
  }

  private overlayWidth(): string {
    return `min(calc(100vw - 2 * ${PANEL_MARGIN}), ${this.panel.width()}px)`;
  }

  ngOnDestroy() {
    this.overlayRef.dispose();
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(event: KeyboardEvent) {
    const isToggle = (event.ctrlKey || event.metaKey) && event.key === 'i';

    if (isToggle) {
      event.preventDefault();
      this.panel.toggle();
    }
  }
}
