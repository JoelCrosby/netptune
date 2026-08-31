import { Overlay, OverlayRef, OverlayConfig } from '@angular/cdk/overlay';
import { CdkPortal } from '@angular/cdk/portal';
import { cn } from '../button/button.variants';
import {
  Component,
  HostListener,
  computed,
  inject,
  input,
  OnDestroy,
  output,
  signal,
  viewChild,
} from '@angular/core';

export type DropdownMenuXPosition = 'before' | 'after';

/**
 * A menu hangs off a trigger element, or off a bare point when it is opened by
 * a right click and should appear under the pointer.
 */
export type DropdownMenuOrigin = HTMLElement | { x: number; y: number };

@Component({
  selector: 'app-dropdown-menu',
  host: { class: 'contents' },
  template: `
    <ng-template cdkPortal>
      <div [class]="className()" [attr.role]="panelRole()">
        <ng-content />
      </div>
    </ng-template>
  `,
  styles: [
    `
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
    `,
  ],
  imports: [CdkPortal],
})
export class DropdownMenuComponent implements OnDestroy {
  private overlay = inject(Overlay);

  readonly xPosition = input<DropdownMenuXPosition>('after');
  readonly panelRole = input('menu');
  /** Padding of the panel itself, dropped by content that draws its own edges. */
  readonly panelClass = input('p-1');
  /** Nudges the panel back into view, for menus opened at an arbitrary point. */
  readonly push = input(false);

  protected readonly className = computed(() => {
    return cn(
      'dropdown-menu min-w-40 rounded-md border border-neutral-200 bg-white shadow-lg dark:border-neutral-700 dark:bg-neutral-900',
      this.panelClass()
    );
  });

  readonly closed = output();

  readonly showing = signal(false);

  private readonly portal = viewChild.required(CdkPortal);
  private overlayRef?: OverlayRef;
  private origin?: DropdownMenuOrigin;

  toggle(origin: DropdownMenuOrigin) {
    if (this.overlayRef?.hasAttached()) {
      this.close();
    } else {
      this.open(origin);
    }
  }

  open(origin: DropdownMenuOrigin) {
    this.origin = origin;
    this.overlayRef = this.overlay.create(this.getOverlayConfig(origin));
    this.overlayRef.attach(this.portal());
    this.overlayRef.backdropClick().subscribe(() => this.close());
    this.overlayRef.keydownEvents().subscribe((event) => {
      if (event.key !== 'Escape') return;

      event.preventDefault();
      this.closeAndFocusTrigger();
    });
    this.showing.set(true);
  }

  closeAndFocusTrigger() {
    const origin = this.origin;

    this.close();

    if (!origin) return;

    focusTrigger(origin);
  }

  close() {
    const wasShowing = this.showing();

    this.overlayRef?.dispose();
    this.overlayRef = undefined;
    this.showing.set(false);

    if (wasShowing) {
      this.closed.emit();
    }
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }

  @HostListener('window:resize')
  onWinResize() {
    if (this.origin && this.overlayRef?.hasAttached()) {
      this.overlayRef.updatePositionStrategy(
        this.buildPositionStrategy(this.origin)
      );
    }
  }

  private buildPositionStrategy(origin: DropdownMenuOrigin) {
    const isBefore = this.xPosition() === 'before';

    return this.overlay
      .position()
      .flexibleConnectedTo(origin)
      .withPush(this.push())
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

  private getOverlayConfig(origin: DropdownMenuOrigin): OverlayConfig {
    return new OverlayConfig({
      positionStrategy: this.buildPositionStrategy(origin),
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
    });
  }
}

function focusTrigger(origin: DropdownMenuOrigin) {
  if (!(origin instanceof HTMLElement)) return;

  const trigger =
    origin instanceof HTMLButtonElement
      ? origin
      : origin.querySelector('button');

  trigger?.focus();
}
