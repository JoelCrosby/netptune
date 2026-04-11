import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TooltipPosition } from '../../directives/tooltip.directive';

const ARROW_CLASSES: Record<TooltipPosition, string> = {
  top: 'bottom-[-4px] left-1/2 -translate-x-1/2 border-t-[var(--tooltip-bg)] border-x-transparent border-b-transparent border-x-[4px] border-t-[4px] border-b-0',
  bottom:
    'top-[-4px] left-1/2 -translate-x-1/2 border-b-[var(--tooltip-bg)] border-x-transparent border-t-transparent border-x-[4px] border-b-[4px] border-t-0',
  left: 'right-[-4px] top-1/2 -translate-y-1/2 border-l-[var(--tooltip-bg)] border-y-transparent border-r-transparent border-y-[4px] border-l-[4px] border-r-0',
  right:
    'left-[-4px] top-1/2 -translate-y-1/2 border-r-[var(--tooltip-bg)] border-y-transparent border-l-transparent border-y-[4px] border-r-[4px] border-l-0',
};

@Component({
  selector: 'app-tooltip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pointer-events-none" role="tooltip">
      <div
        class="relative rounded-sm bg-black px-2 py-1 text-sm font-medium whitespace-nowrap text-white/90 shadow-md dark:bg-white dark:text-black/70">
        {{ text() }}
        <span class="absolute border-solid" [class]="arrowClass()"></span>
      </div>
    </div>
  `,
})
export class TooltipComponent {
  text = input<string>('');
  position = input<TooltipPosition>('top');

  arrowClass() {
    return ARROW_CLASSES[this.position()];
  }
}
