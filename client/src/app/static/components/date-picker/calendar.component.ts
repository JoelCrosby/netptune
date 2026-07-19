import {
  Component,
  ElementRef,
  computed,
  input,
  linkedSignal,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { IconButtonComponent } from '../button/icon-button.component';
import {
  LucideChevronDown,
  LucideChevronLeft,
  LucideChevronRight,
} from '@lucide/angular';
import {
  CalendarDay,
  addCalendarDays,
  addCalendarMonths,
  calendarDays,
  dateValue,
  makeDate,
  parseDateValue,
  sameCalendarDay,
  startOfCalendarMonth,
} from './date-picker.utils';

const weekdays = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];
const months = Array.from({ length: 12 }, (_, month) => ({
  index: month,
  long: new Intl.DateTimeFormat(undefined, { month: 'long' }).format(
    makeDate(2024, month, 1)
  ),
  short: new Intl.DateTimeFormat(undefined, { month: 'short' }).format(
    makeDate(2024, month, 1)
  ),
}));

@Component({
  selector: 'app-calendar',
  imports: [
    IconButtonComponent,
    LucideChevronDown,
    LucideChevronLeft,
    LucideChevronRight,
  ],
  host: { class: 'block' },
  template: `
    <div class="w-72 p-3">
      <div class="mb-3 flex items-center justify-between">
        <button
          app-icon-button
          type="button"
          class="h-8 w-8"
          aria-label="Previous month"
          [disabled]="previousDisabled()"
          (click)="changeMonth(-1)">
          <svg lucideChevronLeft class="h-4 w-4"></svg>
        </button>

        <div class="flex items-center gap-1 text-sm font-semibold">
          <div class="relative">
            <button
              #monthTrigger
              type="button"
              class="hover:bg-muted/60 focus-visible:ring-foreground/30 flex h-8 min-w-26 cursor-pointer items-center justify-center gap-1 rounded-sm px-2 outline-none focus-visible:ring-2"
              aria-haspopup="listbox"
              aria-label="Choose month"
              [attr.aria-expanded]="monthMenuOpen()"
              (click)="toggleMonthMenu()">
              <span>{{ currentMonthName() }}</span>
              <svg lucideChevronDown class="h-3.5 w-3.5 opacity-60"></svg>
            </button>

            @if (monthMenuOpen()) {
              <button
                type="button"
                class="fixed inset-0 z-10 cursor-default"
                tabindex="-1"
                aria-hidden="true"
                (click)="closeMonthMenu()"></button>
              <div
                class="border-border bg-background absolute top-full left-1/2 z-20 mt-1 grid w-52 -translate-x-1/2 grid-cols-3 gap-1 rounded-md border p-1 shadow-xl"
                role="listbox"
                aria-label="Month"
                (keydown.escape.prevent.stop)="closeMonthMenu()">
                @for (month of months; track month.index) {
                  <button
                    type="button"
                    class="hover:bg-muted/60 focus-visible:ring-foreground/30 h-8 cursor-pointer rounded-sm px-2 text-sm outline-none focus-visible:ring-2 disabled:pointer-events-none disabled:opacity-30"
                    [class.bg-primary]="month.index === currentMonth()"
                    [class.text-white]="month.index === currentMonth()"
                    [disabled]="monthDisabled(month.index)"
                    [attr.aria-selected]="month.index === currentMonth()"
                    role="option"
                    (click)="selectMonth(month.index)">
                    {{ month.short }}
                  </button>
                }
              </div>
            }
          </div>
          <span aria-live="polite">{{ currentYear() }}</span>
        </div>

        <button
          app-icon-button
          type="button"
          class="h-8 w-8"
          aria-label="Next month"
          [disabled]="nextDisabled()"
          (click)="changeMonth(1)">
          <svg lucideChevronRight class="h-4 w-4"></svg>
        </button>
      </div>

      <div
        #grid
        class="grid grid-cols-7 gap-1"
        role="grid"
        [attr.aria-label]="monthLabel()">
        @for (weekday of weekdays; track weekday) {
          <div
            class="text-muted-foreground flex h-8 items-center justify-center text-xs font-medium"
            role="columnheader">
            {{ weekday }}
          </div>
        }

        @for (day of days(); track day.value) {
          <button
            type="button"
            class="hover:bg-muted/60 focus-visible:ring-primary relative flex h-8 w-8 items-center justify-center rounded-sm text-sm outline-none focus-visible:ring-2 disabled:pointer-events-none disabled:opacity-30"
            [class.bg-primary]="day.selected"
            [class.text-white]="day.selected"
            [class.text-muted-foreground]="!day.currentMonth && !day.selected"
            [class.ring-1]="day.today && !day.selected"
            [class.ring-primary]="day.today && !day.selected"
            [attr.data-date]="day.value"
            [attr.aria-label]="dayAriaLabel(day)"
            [attr.aria-selected]="day.selected"
            [attr.tabindex]="isActive(day) ? 0 : -1"
            [disabled]="day.disabled"
            role="gridcell"
            (click)="select(day)"
            (keydown)="handleKeydown(day, $event)">
            {{ day.date.getDate() }}
          </button>
        }
      </div>
    </div>
  `,
})
export class CalendarComponent {
  readonly value = input('');
  readonly min = input<string>();
  readonly max = input<string>();
  readonly dateSelected = output<string>();

  private readonly grid = viewChild.required<ElementRef<HTMLElement>>('grid');
  private readonly monthTrigger =
    viewChild.required<ElementRef<HTMLButtonElement>>('monthTrigger');
  private readonly selectedDate = computed(() => parseDateValue(this.value()));
  private readonly minDate = computed(() => parseDateValue(this.min()));
  private readonly maxDate = computed(() => parseDateValue(this.max()));
  private readonly initialDate = computed(() =>
    this.constrainDate(this.selectedDate() ?? this.today())
  );
  private readonly viewDate = linkedSignal(() =>
    startOfCalendarMonth(this.initialDate())
  );
  private readonly activeDate = linkedSignal(() => this.initialDate());
  protected readonly monthMenuOpen = signal(false);

  protected readonly weekdays = weekdays;
  protected readonly months = months;
  protected readonly days = computed(() =>
    calendarDays(
      this.viewDate(),
      this.selectedDate(),
      this.minDate(),
      this.maxDate()
    )
  );
  protected readonly monthLabel = computed(() =>
    new Intl.DateTimeFormat(undefined, {
      month: 'long',
      year: 'numeric',
    }).format(this.viewDate())
  );
  protected readonly currentMonth = computed(() => this.viewDate().getMonth());
  protected readonly currentMonthName = computed(
    () => months[this.currentMonth()].long
  );
  protected readonly currentYear = computed(() =>
    this.viewDate().getFullYear()
  );
  protected readonly previousDisabled = computed(() => {
    const minDate = this.minDate();
    const previousMonthEnd = makeDate(
      this.viewDate().getFullYear(),
      this.viewDate().getMonth(),
      0
    );
    return minDate !== null && previousMonthEnd < minDate;
  });
  protected readonly nextDisabled = computed(() => {
    const maxDate = this.maxDate();
    const nextMonthStart = makeDate(
      this.viewDate().getFullYear(),
      this.viewDate().getMonth() + 1,
      1
    );
    return maxDate !== null && nextMonthStart > maxDate;
  });

  focusActiveDate(): void {
    this.focusDate(this.activeDate());
  }

  protected changeMonth(change: number): void {
    this.monthMenuOpen.set(false);
    const nextView = addCalendarMonths(this.viewDate(), change);
    const nextActive = this.constrainDate(
      makeDate(nextView.getFullYear(), nextView.getMonth(), 1)
    );
    this.viewDate.set(startOfCalendarMonth(nextView));
    this.activeDate.set(nextActive);
    this.focusDate(nextActive);
  }

  protected toggleMonthMenu(): void {
    this.monthMenuOpen.update((open) => !open);
  }

  protected closeMonthMenu(): void {
    this.monthMenuOpen.set(false);
    this.monthTrigger().nativeElement.focus();
  }

  protected selectMonth(month: number): void {
    const target = this.constrainDate(
      makeDate(this.viewDate().getFullYear(), month, 1)
    );
    this.monthMenuOpen.set(false);
    this.viewDate.set(startOfCalendarMonth(target));
    this.activeDate.set(target);
    this.focusDate(target);
  }

  protected monthDisabled(month: number): boolean {
    const year = this.viewDate().getFullYear();
    const start = makeDate(year, month, 1);
    const end = makeDate(year, month + 1, 0);
    const minDate = this.minDate();
    const maxDate = this.maxDate();
    return (
      (minDate !== null && end < minDate) ||
      (maxDate !== null && start > maxDate)
    );
  }

  protected select(day: CalendarDay): void {
    if (!day.disabled) {
      this.dateSelected.emit(day.value);
    }
  }

  protected isActive(day: CalendarDay): boolean {
    return sameCalendarDay(day.date, this.activeDate());
  }

  protected dayAriaLabel(day: CalendarDay): string {
    const label = new Intl.DateTimeFormat(undefined, {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    }).format(day.date);
    const states = [
      day.today ? 'Today' : '',
      day.selected ? 'Selected' : '',
    ].filter(Boolean);

    return states.length > 0 ? `${label}, ${states.join(', ')}` : label;
  }

  protected handleKeydown(day: CalendarDay, event: KeyboardEvent): void {
    const target = this.keyboardTarget(day.date, event);

    if (target === null) {
      return;
    }

    event.preventDefault();
    this.moveFocus(target);
  }

  private keyboardTarget(date: Date, event: KeyboardEvent): Date | null {
    switch (event.key) {
      case 'ArrowLeft':
        return this.nextEnabledDate(addCalendarDays(date, -1), -1);
      case 'ArrowRight':
        return this.nextEnabledDate(addCalendarDays(date, 1), 1);
      case 'ArrowUp':
        return this.nextEnabledDate(addCalendarDays(date, -7), -7);
      case 'ArrowDown':
        return this.nextEnabledDate(addCalendarDays(date, 7), 7);
      case 'Home':
        return this.nextEnabledDate(addCalendarDays(date, -date.getDay()), 1);
      case 'End':
        return this.nextEnabledDate(
          addCalendarDays(date, 6 - date.getDay()),
          -1
        );
      case 'PageUp':
        return this.constrainDate(addCalendarMonths(date, -1));
      case 'PageDown':
        return this.constrainDate(addCalendarMonths(date, 1));
      default:
        return null;
    }
  }

  private moveFocus(date: Date): void {
    this.activeDate.set(date);

    if (
      date.getMonth() !== this.viewDate().getMonth() ||
      date.getFullYear() !== this.viewDate().getFullYear()
    ) {
      this.viewDate.set(startOfCalendarMonth(date));
    }

    this.focusDate(date);
  }

  private focusDate(date: Date): void {
    setTimeout(() => {
      const button = this.grid().nativeElement.querySelector<HTMLElement>(
        `[data-date="${dateValue(date)}"]`
      );
      button?.focus();
    });
  }

  private nextEnabledDate(date: Date, step: number): Date {
    let candidate = this.constrainDate(date);

    for (let attempt = 0; attempt < 366; attempt += 1) {
      if (!this.isDisabled(candidate)) {
        return candidate;
      }

      candidate = this.constrainDate(addCalendarDays(candidate, step));
    }

    return this.activeDate();
  }

  private constrainDate(date: Date): Date {
    const minDate = this.minDate();
    const maxDate = this.maxDate();

    if (minDate !== null && date < minDate) {
      return minDate;
    }

    if (maxDate !== null && date > maxDate) {
      return maxDate;
    }

    return date;
  }

  private isDisabled(date: Date): boolean {
    const minDate = this.minDate();
    const maxDate = this.maxDate();
    return (
      (minDate !== null && date < minDate) ||
      (maxDate !== null && date > maxDate)
    );
  }

  private today(): Date {
    const now = new Date();
    return makeDate(now.getFullYear(), now.getMonth(), now.getDate());
  }
}
