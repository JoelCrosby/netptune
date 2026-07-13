import { Component, computed, input, output } from '@angular/core';
import { LucideDynamicIcon, LucideIconInput } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-filter-action-button',
  imports: [LucideDynamicIcon, TooltipDirective, BadgeComponent],
  template: `
    <button [class]="class()" [appTooltip]="label()" (click)="action.emit()">
      <svg size="20" class="outline-none" [lucideIcon]="icon()"></svg>
      @if (count()) {
        <app-badge color="primary" class="absolute -top-1.5 -right-1.5">
          {{ count() }}
        </app-badge>
      } @else if (dot()) {
        <span
          class="bg-primary absolute -top-0.5 -right-0.5 h-2 w-2 rounded-full">
        </span>
      }
    </button>
  `,
})
export class FilterActionButtonComponent {
  readonly label = input.required<string>();
  readonly icon = input.required<LucideIconInput>();
  readonly color = input<'primary' | 'warn'>();
  readonly count = input<number>(0);
  readonly dot = input<boolean>(false);

  readonly action = output();

  readonly class = computed(() => {
    const base =
      'hover:bg-foreground/10 text-foreground/70 relative flex h-9 cursor-pointer appearance-none items-center rounded-sm px-4 transition-[background-color,color] duration-[140ms] ease-in-out outline-none active:outline-none';

    const color = this.color();

    if (color === 'warn') {
      return `${base} text-red-400 bg-red-500/10 hover:bg-red-500/20`;
    }

    if (color === 'primary') {
      return `${base} text-primary-400 bg-primary-500/10 hover:bg-primary-500/20`;
    }

    return base;
  });
}
