import { Component, computed, input } from '@angular/core';

export type PopoverSurfaceSize = 'compact' | 'sheet' | 'wide';

/** How long `.menu-scale-out` in `styles/menu.css` runs for. */
export const menuExitMs = 120;
export type PopoverSurfaceEnterFrom =
  | 'none'
  | 'top'
  | 'top-right'
  | 'right'
  | 'bottom-right'
  | 'bottom'
  | 'bottom-left'
  | 'left'
  | 'top-left';

@Component({
  selector: 'app-popover-surface',
  styles: `
    [data-enter-from='top'] {
      --menu-enter-translate-y: -4px;
      transform-origin: top;
    }

    [data-enter-from='top-right'] {
      --menu-enter-translate-x: 4px;
      --menu-enter-translate-y: -4px;
      transform-origin: top right;
    }

    [data-enter-from='right'] {
      --menu-enter-translate-x: 4px;
      transform-origin: right;
    }

    [data-enter-from='bottom-right'] {
      --menu-enter-translate-x: 4px;
      --menu-enter-translate-y: 4px;
      transform-origin: bottom right;
    }

    [data-enter-from='bottom'] {
      --menu-enter-translate-y: 4px;
      transform-origin: bottom;
    }

    [data-enter-from='bottom-left'] {
      --menu-enter-translate-x: -4px;
      --menu-enter-translate-y: 4px;
      transform-origin: bottom left;
    }

    [data-enter-from='left'] {
      --menu-enter-translate-x: -4px;
      transform-origin: left;
    }

    [data-enter-from='top-left'] {
      --menu-enter-translate-x: -4px;
      --menu-enter-translate-y: -4px;
      transform-origin: top left;
    }
  `,
  template: `
    <div
      class="custom-scroll border-border bg-background flex flex-col overflow-x-hidden border text-left shadow-xl dark:shadow-black/60"
      [attr.data-enter-from]="enterFrom()"
      [class]="surfaceClass()">
      <ng-content />
    </div>
  `,
})
export class PopoverSurfaceComponent {
  readonly size = input<PopoverSurfaceSize>('wide');
  readonly enterFrom = input<PopoverSurfaceEnterFrom>('none');

  /**
   * Plays the exit animation. The owner of the overlay is responsible for
   * detaching it once the animation has had `menuExitMs` to run.
   */
  readonly leaving = input(false);

  protected readonly surfaceClass = computed(() => {
    const animation = this.leaving() ? 'menu-scale-out' : 'menu-scale-in';

    return `${animation} ${this.sizeClass()}`;
  });

  private readonly sizeClass = computed(() => {
    switch (this.size()) {
      case 'compact':
        return 'h-full w-61.5 rounded-sm';
      // Takes its width from the overlay and clips its own corners, so the
      // rows flush to the edges stay inside the radius.
      case 'sheet':
        return 'h-full w-full overflow-y-hidden rounded';
      default:
        return 'max-h-[80vh] max-w-120 min-w-100 overflow-y-auto rounded';
    }
  });
}
