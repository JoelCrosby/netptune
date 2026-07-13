import { Component, input } from '@angular/core';

export type PopoverSurfaceSize = 'compact' | 'wide';
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
  template: `<div
    class="menu-scale-in custom-scroll border-border bg-background flex flex-col overflow-x-hidden border text-left shadow-xl dark:shadow-black/60"
    [attr.data-enter-from]="enterFrom()"
    [class]="
      size() === 'compact'
        ? 'h-full w-61.5 rounded-sm'
        : 'max-h-[80vh] max-w-120 min-w-100 overflow-y-auto rounded'
    ">
    <ng-content />
  </div>`,
})
export class PopoverSurfaceComponent {
  readonly size = input<PopoverSurfaceSize>('wide');
  readonly enterFrom = input<PopoverSurfaceEnterFrom>('none');
}
