import { CdkTrapFocus } from '@angular/cdk/a11y';
import { Overlay, OverlayConfig, OverlayRef } from '@angular/cdk/overlay';
import { CdkPortal } from '@angular/cdk/portal';
import {
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  computed,
  inject,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { LucideCalendarDays, LucideChevronDown } from '@lucide/angular';
import { cn } from '../button/button.variants';
import { CalendarComponent } from './calendar.component';
import { parseDateValue } from './date-picker.utils';

export type DatePickerAppearance = 'field' | 'bare';

@Component({
  selector: 'app-date-picker',
  imports: [
    CalendarComponent,
    CdkPortal,
    CdkTrapFocus,
    LucideCalendarDays,
    LucideChevronDown,
  ],
  host: { class: 'block min-w-0' },
  template: `
    <button
      #trigger
      type="button"
      [id]="controlId()"
      [class]="triggerClass()"
      [disabled]="disabled()"
      [attr.aria-label]="ariaLabel()"
      [attr.aria-expanded]="open()"
      [attr.aria-required]="required()"
      aria-haspopup="dialog"
      (click)="toggle()">
      <svg lucideCalendarDays class="h-4 w-4 shrink-0 opacity-70"></svg>
      <span
        class="min-w-0 flex-1 truncate text-left"
        [class.text-muted-foreground]="!value()">
        {{ displayText() }}
      </span>
      <svg lucideChevronDown class="h-4 w-4 shrink-0 opacity-70"></svg>
    </button>

    <ng-template cdkPortal>
      <div
        cdkTrapFocus
        [cdkTrapFocusAutoCapture]="true"
        class="menu-scale-in border-border bg-background rounded-md border shadow-xl dark:shadow-black/60"
        role="dialog"
        [attr.aria-label]="ariaLabel()">
        <app-calendar
          [value]="value()"
          [min]="min()"
          [max]="max()"
          (dateSelected)="selectDate($event)" />
      </div>
    </ng-template>
  `,
})
export class DatePickerComponent implements OnDestroy {
  readonly value = model('');
  readonly controlId = input('');
  readonly label = input('');
  readonly placeholder = input('Select date');
  readonly ariaLabel = input('Choose date');
  readonly min = input<string>();
  readonly max = input<string>();
  readonly disabled = input(false);
  readonly required = input(false);
  readonly appearance = input<DatePickerAppearance>('field');
  readonly buttonClass = input('');
  readonly touched = output();
  readonly open = signal(false);

  private readonly overlay = inject(Overlay);
  private readonly portal = viewChild.required(CdkPortal);
  private readonly trigger =
    viewChild.required<ElementRef<HTMLButtonElement>>('trigger');
  private readonly calendar = viewChild(CalendarComponent);
  private overlayRef?: OverlayRef;

  protected readonly triggerClass = computed(() =>
    cn(
      'flex min-w-0 cursor-pointer items-center gap-2 text-sm outline-none transition-colors focus-visible:ring-2 focus-visible:ring-foreground/30 disabled:pointer-events-none disabled:opacity-50',
      this.appearance() === 'field'
        ? 'border-border bg-form-field-background h-10 w-full rounded-sm border-2 px-3'
        : 'h-10 w-full px-3',
      this.buttonClass()
    )
  );
  protected readonly displayText = computed(() => {
    const date = parseDateValue(this.value());
    const value = date
      ? new Intl.DateTimeFormat(undefined, {
          day: 'numeric',
          month: 'short',
          year: 'numeric',
        }).format(date)
      : this.placeholder();
    const label = this.label();

    return label ? `${label}: ${value}` : value;
  });

  toggle(): void {
    if (this.open()) {
      this.close();
    } else {
      this.show();
    }
  }

  show(): void {
    if (this.disabled() || this.open()) {
      return;
    }

    const overlayRef = this.overlay.create(this.overlayConfig());
    this.overlayRef = overlayRef;
    overlayRef.attach(this.portal());
    overlayRef.backdropClick().subscribe(() => this.close());
    overlayRef.keydownEvents().subscribe((event) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        this.close();
      }
    });
    overlayRef.detachments().subscribe(() => {
      if (this.overlayRef === overlayRef) {
        this.overlayRef = undefined;
        this.open.set(false);
      }
    });
    this.open.set(true);
    setTimeout(() => this.calendar()?.focusActiveDate());
  }

  close(markTouched = true): void {
    const wasOpen = this.open();
    this.overlayRef?.dispose();
    this.overlayRef = undefined;
    this.open.set(false);

    if (wasOpen && markTouched) {
      this.touched.emit();
      this.trigger().nativeElement.focus();
    }
  }

  ngOnDestroy(): void {
    this.close(false);
  }

  @HostListener('window:resize')
  updatePosition(): void {
    this.overlayRef?.updatePosition();
  }

  protected selectDate(value: string): void {
    this.value.set(value);
    this.close();
  }

  private overlayConfig(): OverlayConfig {
    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(this.trigger())
      .withPush(true)
      .withPositions([
        {
          originX: 'start',
          originY: 'bottom',
          overlayX: 'start',
          overlayY: 'top',
          offsetY: 4,
        },
        {
          originX: 'start',
          originY: 'top',
          overlayX: 'start',
          overlayY: 'bottom',
          offsetY: -4,
        },
      ]);

    return new OverlayConfig({
      positionStrategy,
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
      disposeOnNavigation: true,
    });
  }
}
