import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  AfterViewInit,
  Component,
  HostListener,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  viewChild,
} from '@angular/core';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiAssistantPanelComponent } from './ai-assistant-panel.component';

const NAVBAR_HEIGHT = '60px';
const PANEL_MARGIN = '1rem';

@Component({
  selector: 'app-ai-assistant',
  imports: [AiAssistantPanelComponent],
  template: `
    <ng-template #panelTmpl>
      <app-ai-assistant-panel
        class="border-border overflow-hidden rounded-2xl border shadow-lg" />
    </ng-template>
  `,
})
export class AiAssistantComponent implements AfterViewInit, OnDestroy {
  private readonly panelTmpl =
    viewChild.required<TemplateRef<unknown>>('panelTmpl');

  private readonly overlay = inject(Overlay);
  private readonly vcr = inject(ViewContainerRef);
  private readonly overlayRef: OverlayRef;
  private portal!: TemplatePortal;

  protected readonly assistant = inject(AiAssistantService);

  constructor() {
    this.overlayRef = this.overlay.create({
      hasBackdrop: false,
      positionStrategy: this.overlay
        .position()
        .global()
        .right(PANEL_MARGIN)
        .top(`calc(${NAVBAR_HEIGHT} + ${PANEL_MARGIN})`),
      scrollStrategy: this.overlay.scrollStrategies.noop(),
      width: `min(calc(100vw - 2 * ${PANEL_MARGIN}), 26rem)`,
      height: `calc(100% - ${NAVBAR_HEIGHT} - 2 * ${PANEL_MARGIN})`,
    });

    effect(() => {
      const isOpen = this.assistant.isOverlayOpen();

      if (isOpen && this.portal && !this.overlayRef.hasAttached()) {
        this.overlayRef.attach(this.portal);

        return;
      }

      if (!isOpen && this.overlayRef.hasAttached()) {
        this.overlayRef.detach();
      }
    });
  }

  ngAfterViewInit() {
    this.portal = new TemplatePortal(this.panelTmpl(), this.vcr);
  }

  ngOnDestroy() {
    this.overlayRef.dispose();
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(event: KeyboardEvent) {
    const isToggle = (event.ctrlKey || event.metaKey) && event.key === 'i';

    if (isToggle) {
      event.preventDefault();
      this.assistant.toggle();
    }
  }
}
