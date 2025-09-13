import { Overlay, OverlayRef, OverlayConfig } from '@angular/cdk/overlay';
import { CdkPortal } from '@angular/cdk/portal';
import { Component, ChangeDetectionStrategy, HostListener, inject, input, viewChild } from '@angular/core';
import { ɵɵCdkPortal } from '@angular/cdk/dialog';

@Component({
    selector: 'app-form-select-dropdown',
    template: `
    <ng-template cdkPortal>
      <ng-content />
    </ng-template>
  `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ɵɵCdkPortal]
})
export class FormSelectDropdownComponent {
  private overlay = inject(Overlay);

  readonly reference = input.required<HTMLElement>();
  readonly portal = viewChild.required(CdkPortal);

  overlayRef?: OverlayRef;
  showing = false;

  show() {
    this.overlayRef = this.overlay.create(this.getOverlayConfig());
    this.overlayRef.attach(this.portal());
    this.setWidth();
    this.overlayRef.backdropClick().subscribe(() => this.hide());
    this.showing = true;
  }

  hide() {
    this.overlayRef?.detach();
    this.showing = false;
  }

  @HostListener('window:resize')
  onWinResize() {
    this.setWidth();
  }

  private setWidth() {
    if (!this.overlayRef) {
      return;
    }

    const refRect = this.reference().getBoundingClientRect();
    this.overlayRef.updateSize({ width: refRect.width });
  }

  private getOverlayConfig(): OverlayConfig {
    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(this.reference())
      .withPush(false)
      .withPositions([
        {
          originX: 'start',
          originY: 'bottom',
          overlayX: 'start',
          overlayY: 'top',
        },
        {
          originX: 'start',
          originY: 'top',
          overlayX: 'start',
          overlayY: 'bottom',
        },
      ]);

    return new OverlayConfig({
      positionStrategy: positionStrategy,
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
    });
  }
}
