import { Component, computed, input, output } from '@angular/core';
import {
  LucideChevronDown,
  LucideDynamicIcon,
  LucideIconInput,
  LucideX,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { cn } from '../button/button.variants';

// The text only appears inside a toolbar that declares itself a container and has
// room for it, so a caller with no `@container` ancestor keeps the icon-only form.
const SHOW_TEXT = '@7xl:inline';
const SHOW_TEXT_BLOCK = '@7xl:block';
const SHOW_TEXT_FLEX = '@7xl:flex';
const HIDE_WITH_TEXT = '@7xl:hidden';

@Component({
  selector: 'app-filter-action-button',
  imports: [
    LucideDynamicIcon,
    LucideChevronDown,
    LucideX,
    TooltipDirective,
    BadgeComponent,
  ],
  host: { class: 'inline-flex' },
  template: `
    <div [class]="containerClass()">
      <button
        type="button"
        [class]="class()"
        [appTooltip]="label()"
        [attr.aria-expanded]="expanded()"
        (click)="action.emit()">
        <span class="sr-only">{{ label() }}</span>
        <svg
          size="20"
          class="shrink-0 outline-none"
          aria-hidden="true"
          [lucideIcon]="icon()"></svg>
        @if (displayText(); as text) {
          <span [class]="textClass" [attr.aria-hidden]="value() ? null : true">
            {{ text }}
          </span>
        }

        @if (showChevron()) {
          <svg
            lucideChevronDown
            [class]="chevronClass"
            [class.rotate-180]="expanded()"
            aria-hidden="true"></svg>
        }

        @if (count()) {
          <app-badge color="primary" [class]="badgeClass()">
            {{ count() }}
          </app-badge>
        } @else if (dot()) {
          <span [class]="dotClass()" aria-hidden="true"></span>
        }
      </button>

      @if (clearable()) {
        <button
          type="button"
          [class]="clearClass()"
          [attr.aria-label]="clearLabel()"
          (click)="cleared.emit()">
          <svg lucideX class="h-3.5 w-3.5" aria-hidden="true"></svg>
        </button>
      }
    </div>
  `,
})
export class FilterActionButtonComponent {
  readonly label = input.required<string>();
  readonly icon = input.required<LucideIconInput>();
  readonly color = input<'primary' | 'warn'>();
  readonly shortLabel = input<string | null>(null);
  readonly value = input<string | null>(null);
  readonly count = input<number>(0);
  readonly dot = input<boolean>(false);
  readonly expanded = input<boolean | null>(null);
  readonly clearable = input<boolean>(false);
  readonly clearLabel = input(
    $localize`:Accessible label for the button that clears the current filter:Clear filter`
  );

  readonly action = output();
  readonly cleared = output();

  protected readonly textClass = cn(
    'hidden max-w-50 truncate text-sm font-medium',
    SHOW_TEXT
  );

  protected readonly chevronClass = cn(
    'hidden h-3.5 w-3.5 shrink-0 opacity-60 transition-transform',
    SHOW_TEXT_BLOCK
  );

  protected readonly displayText = computed(() => {
    return this.value() ?? this.shortLabel();
  });

  protected readonly showChevron = computed(() => {
    const hasMenu = this.expanded() !== null;

    return hasMenu && !this.clearable() && !!this.displayText();
  });

  protected readonly badgeClass = computed(() => {
    const base = 'absolute -top-1.5 -right-1.5';

    return this.displayText() ? cn(base, HIDE_WITH_TEXT) : base;
  });

  protected readonly dotClass = computed(() => {
    const base = 'bg-primary absolute -top-0.5 -right-0.5 h-2 w-2 rounded-full';

    return this.displayText() ? cn(base, HIDE_WITH_TEXT) : base;
  });

  protected readonly containerClass = computed(() => {
    const base = 'inline-flex h-9 items-center rounded-lg';

    const color = this.color();

    if (color === 'warn') {
      return cn(base, 'bg-warn/10 ring-1 ring-warn/30 ring-inset');
    }

    if (color === 'primary') {
      return cn(base, 'bg-primary/10 ring-1 ring-primary/30 ring-inset');
    }

    return base;
  });

  protected readonly class = computed(() => {
    const base =
      'relative flex h-9 cursor-pointer appearance-none items-center gap-2 rounded-lg px-3 transition-[background-color,color] duration-[140ms] ease-in-out outline-none focus-visible:ring-2 focus-visible:ring-foreground active:outline-none';

    const shape = this.clearable()
      ? '@7xl:rounded-r-none @7xl:pr-1'
      : '';

    const color = this.color();

    if (color === 'warn') {
      return cn(base, shape, 'text-warn hover:bg-warn/15');
    }

    if (color === 'primary') {
      return cn(base, shape, 'text-primary hover:bg-primary/15');
    }

    return cn(
      base,
      shape,
      'text-foreground/70 hover:bg-foreground/10 hover:text-foreground'
    );
  });

  protected readonly clearClass = computed(() => {
    const base = cn(
      'hidden h-9 w-7 shrink-0 cursor-pointer items-center justify-center rounded-r-lg outline-none focus-visible:ring-2 focus-visible:ring-foreground',
      SHOW_TEXT_FLEX
    );

    const color = this.color();

    if (color === 'warn') {
      return cn(base, 'text-warn hover:bg-warn/20');
    }

    if (color === 'primary') {
      return cn(base, 'text-primary hover:bg-primary/20');
    }

    return cn(
      base,
      'text-foreground/70 hover:bg-foreground/10 hover:text-foreground'
    );
  });
}
