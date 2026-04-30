import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { LucideDynamicIcon, LucideIconInput } from '@lucide/angular';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-board-group-header-action',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideDynamicIcon, TooltipDirective],
  template: `
    <button [class]="class()" [appTooltip]="label()" (click)="action.emit()">
      <svg size="20" class="outline-none" [lucideIcon]="icon()"></svg>
    </button>
  `,
})
export class BoardGroupHeaderActionComponent {
  label = input.required<string>();
  icon = input.required<LucideIconInput>();
  color = input<'warn' | 'primary'>();

  action = output();

  class = computed(() => {
    let base =
      'hover:bg-foreground/10 text-foreground/70 flex h-9 cursor-pointer appearance-none items-center rounded-sm px-4 transition-[background-color,color] duration-[140ms] ease-in-out outline-none active:outline-none';

    const color = this.color();

    if (color === 'warn') {
      base = `${base} text-red-400 bg-red-500/10 hover:bg-red-500/20`;
    }

    if (color === 'primary') {
      base = `${base} text-primary-400 bg-primary-500/10 hover:bg-primary-500/20`;
    }

    return base;
  });
}
