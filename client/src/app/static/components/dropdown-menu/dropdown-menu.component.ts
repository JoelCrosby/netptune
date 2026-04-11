import { Overlay, OverlayRef, OverlayConfig } from '@angular/cdk/overlay';
import { CdkPortal } from '@angular/cdk/portal';
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  inject,
  input,
  viewChild,
} from '@angular/core';

export type DropdownMenuXPosition = 'before' | 'after';

@Component({
  selector: 'app-dropdown-menu',
  template: `
    <ng-template cdkPortal>
      <div
        class="dropdown-menu min-w-40 rounded-md border border-neutral-200 bg-white p-1 shadow-lg dark:border-neutral-700 dark:bg-neutral-900"
        role="menu">
        <ng-content />
      </div>
    </ng-template>
  `,
  styles: [`
    @keyframes dropdown-in {
      from {
        opacity: 0;
        transform: scale(0.95) translateY(-4px);
      }
      to {
        opacity: 1;
        transform: scale(1) translateY(0);
      }
    }

    .dropdown-menu {
      animation: dropdown-in 120ms ease-out;
      transform-origin: top;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CdkPortal],
})
export class DropdownMenuComponent {
  private overlay = inject(Overlay);

  readonly xPosition = input<DropdownMenuXPosition>('after');

  private readonly portal = viewChild.required(CdkPortal);
  private overlayRef?: OverlayRef;
  private origin?: HTMLElement;

  toggle(origin: HTMLElement) {
    if (this.overlayRef?.hasAttached()) {
      this.close();
    } else {
      this.open(origin);
    }
  }

  open(origin: HTMLElement) {
    this.origin = origin;
    this.overlayRef = this.overlay.create(this.getOverlayConfig(origin));
    this.overlayRef.attach(this.portal());
    this.overlayRef.backdropClick().subscribe(() => this.close());
  }

  close() {
    this.overlayRef?.detach();
  }

  @HostListener('window:resize')
  onWinResize() {
    if (this.origin && this.overlayRef?.hasAttached()) {
      this.overlayRef.updatePositionStrategy(
        this.buildPositionStrategy(this.origin)
      );
    }
  }

  private buildPositionStrategy(origin: HTMLElement) {
    const isBefore = this.xPosition() === 'before';

    return this.overlay
      .position()
      .flexibleConnectedTo(origin)
      .withPush(false)
      .withPositions([
        {
          originX: isBefore ? 'end' : 'start',
          originY: 'bottom',
          overlayX: isBefore ? 'end' : 'start',
          overlayY: 'top',
          offsetY: 4,
        },
        {
          originX: isBefore ? 'end' : 'start',
          originY: 'top',
          overlayX: isBefore ? 'end' : 'start',
          overlayY: 'bottom',
          offsetY: -4,
        },
      ]);
  }

  private getOverlayConfig(origin: HTMLElement): OverlayConfig {
    return new OverlayConfig({
      positionStrategy: this.buildPositionStrategy(origin),
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
    });
  }
}
