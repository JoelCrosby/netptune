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
  selector: 'app-task-list-filter-action',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideDynamicIcon, TooltipDirective],
  template: `
    <button [class]="class()" [appTooltip]="label()" (click)="action.emit()">
      <svg size="20" class="outline-none" [lucideIcon]="icon()"></svg>
    </button>
  `,
})
export class TaskListFilterActionComponent {
  readonly label = input.required<string>();
  readonly icon = input.required<LucideIconInput>();
  readonly color = input<'primary'>();

  readonly action = output();

  readonly class = computed(() => {
    const base =
      'hover:bg-foreground/10 text-foreground/70 flex h-9 cursor-pointer appearance-none items-center rounded-sm px-4 transition-[background-color,color] duration-[140ms] ease-in-out outline-none active:outline-none';

    if (this.color() === 'primary') {
      return `${base} text-primary-400 bg-primary-500/10 hover:bg-primary-500/20`;
    }

    return base;
  });
}
