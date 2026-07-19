import { Component, computed, input, output } from '@angular/core';
import { LucideCalendarDays, LucideChevronDown } from '@lucide/angular';
import {
  cn,
  flatButtonVariants,
  type FlatButtonColor,
} from '../button/button.variants';

@Component({
  selector: 'app-date-dropdown-button',
  imports: [LucideCalendarDays, LucideChevronDown],
  template: `
    <label [class]="className()">
      <svg lucideCalendarDays class="h-4 w-4 shrink-0 opacity-70"></svg>
      <span class="truncate">{{ label() }}: {{ formattedValue() }}</span>
      <svg lucideChevronDown class="h-4 w-4 shrink-0 opacity-70"></svg>
      <input
        type="date"
        class="absolute inset-0 cursor-pointer opacity-0 disabled:cursor-not-allowed"
        [value]="value()"
        [disabled]="disabled()"
        [attr.aria-label]="ariaLabel() || label()"
        (change)="changeValue($event)" />
    </label>
  `,
})
export class DateDropdownButtonComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly ariaLabel = input<string>();
  readonly color = input<FlatButtonColor>('neutral');
  readonly disabled = input(false);
  readonly buttonClass = input('');
  readonly valueChanged = output<string>();

  protected readonly className = computed(() =>
    cn(
      flatButtonVariants({ color: this.color() }),
      'relative cursor-pointer focus-within:ring-2 focus-within:ring-foreground/30',
      this.buttonClass()
    )
  );

  protected readonly formattedValue = computed(() => {
    const date = new Date(`${this.value()}T00:00:00Z`);

    if (Number.isNaN(date.getTime())) {
      return this.value();
    }

    return new Intl.DateTimeFormat(undefined, {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      timeZone: 'UTC',
    }).format(date);
  });

  protected changeValue(event: Event): void {
    const value = (event.target as HTMLInputElement).value;

    if (value) {
      this.valueChanged.emit(value);
    }
  }
}
