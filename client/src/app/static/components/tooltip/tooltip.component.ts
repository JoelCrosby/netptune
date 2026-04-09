import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TooltipPosition } from '../../directives/tooltip.directive';

const ARROW_CLASSES: Record<TooltipPosition, string> = {
  top: 'bottom-[-4px] left-1/2 -translate-x-1/2 border-t-[var(--tooltip-bg)] border-x-transparent border-b-transparent border-x-[4px] border-t-[4px] border-b-0',
  bottom: 'top-[-4px] left-1/2 -translate-x-1/2 border-b-[var(--tooltip-bg)] border-x-transparent border-t-transparent border-x-[4px] border-b-[4px] border-t-0',
  left: 'right-[-4px] top-1/2 -translate-y-1/2 border-l-[var(--tooltip-bg)] border-y-transparent border-r-transparent border-y-[4px] border-l-[4px] border-r-0',
  right: 'left-[-4px] top-1/2 -translate-y-1/2 border-r-[var(--tooltip-bg)] border-y-transparent border-l-transparent border-y-[4px] border-r-[4px] border-l-0',
};

@Component({
  selector: 'app-tooltip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="absolute z-[9999] pointer-events-none"
      role="tooltip"
    >
      <div
        class="relative px-2 py-1 text-xs font-medium rounded-[var(--radius-sm)] shadow-md whitespace-nowrap
               bg-[var(--tooltip-bg,_#1e1e2e)] text-[var(--tooltip-fg,_#cdd6f4)]"
      >
        {{ text() }}
        <span class="absolute border-solid" [class]="arrowClass()"></span>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: contents;
    }
  `],
})
export class TooltipComponent {
  text = input<string>('');
  position = input<TooltipPosition>('top');

  arrowClass() {
    return ARROW_CLASSES[this.position()];
  }
}
