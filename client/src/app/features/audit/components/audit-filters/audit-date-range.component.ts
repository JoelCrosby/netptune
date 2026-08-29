import { Component, input, output } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DateDropdownButtonComponent } from '@static/components/dropdown-menu/date-dropdown-button.component';

@Component({
  selector: 'app-audit-date-range',
  imports: [DateDropdownButtonComponent, IconButtonComponent, LucideX],
  host: { class: 'flex flex-wrap items-center gap-2' },
  template: `
    <app-date-dropdown-button
      i18n-label="Label of the start-date filter"
      label="From"
      i18n-ariaLabel="Accessible label for the audit log start date"
      ariaLabel="Audit log start date"
      buttonClass="min-w-40 justify-between"
      [value]="from()"
      (valueChanged)="fromChanged.emit($event)" />

    @if (from()) {
      <button
        app-icon-button
        type="button"
        i18n-aria-label="
          Accessible label for the button that clears the start-date filter
        "
        aria-label="Clear start date"
        (click)="fromChanged.emit('')">
        <svg lucideX class="text-foreground/50 h-4 w-4"></svg>
      </button>
    }

    <app-date-dropdown-button
      i18n-label="Label of the end-date filter"
      label="To"
      i18n-ariaLabel="Accessible label for the audit log end date"
      ariaLabel="Audit log end date"
      buttonClass="min-w-40 justify-between"
      [value]="to()"
      (valueChanged)="toChanged.emit($event)" />

    @if (to()) {
      <button
        app-icon-button
        type="button"
        i18n-aria-label="
          Accessible label for the button that clears the end-date filter
        "
        aria-label="Clear end date"
        (click)="toChanged.emit('')">
        <svg lucideX class="text-foreground/50 h-4 w-4"></svg>
      </button>
    }
  `,
})
export class AuditDateRangeComponent {
  readonly from = input('');
  readonly to = input('');

  readonly fromChanged = output<string>();
  readonly toChanged = output<string>();
}
