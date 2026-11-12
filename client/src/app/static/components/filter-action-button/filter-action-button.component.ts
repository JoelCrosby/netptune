import { Component, computed, input, output } from '@angular/core';
import { LucideDynamicIcon, LucideIconInput } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-filter-action-button',
  imports: [LucideDynamicIcon, TooltipDirective, BadgeComponent],
  template: `
    <button
      type="button"
      [class]="class()"
      [appTooltip]="label()"
      [attr.aria-expanded]="expanded()"
      (click)="action.emit()">
      <span class="sr-only">{{ label() }}</span>
      <svg
        size="20"
        class="outline-none"
        aria-hidden="true"
        [lucideIcon]="icon()"></svg>
      @if (count()) {
        <app-badge color="primary" class="absolute -top-1.5 -right-1.5">
          {{ count() }}
        </app-badge>
      } @else if (dot()) {
        <span
          class="bg-primary absolute -top-0.5 -right-0.5 h-2 w-2 rounded-full"
          aria-hidden="true"></span>
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
  readonly expanded = input<boolean | null>(null);

  readonly action = output();

  readonly class = computed(() => {
    const base =
      'hover:bg-foreground/10 text-foreground/70 relative flex h-9 cursor-pointer appearance-none items-center rounded-sm px-4 transition-[background-color,color] duration-[140ms] ease-in-out outline-none focus-visible:ring-2 focus-visible:ring-foreground active:outline-none';

    const color = this.color();

    if (color === 'warn') {
      return cn(base, 'text-warn bg-warn/10 hover:bg-warn/20');
    }

    if (color === 'primary') {
      return cn(base, 'text-primary bg-primary/10 hover:bg-primary/20');
    }

    return base;
  });
}
