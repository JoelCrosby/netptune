import {
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { InlineEditInputComponent } from '../inline-edit-input/inline-edit-input.component';

@Component({
  selector: 'app-page-header-title',
  template: `
    <div class="flex h-full min-w-0 flex-row items-center justify-start">
      @if (logoUrl(); as url) {
        <img
          [src]="url"
          [alt]="title() ?? ''"
          class="border-border mr-3 h-9 w-9 shrink-0 rounded-lg border object-cover" />
      }
      @if (!titleEditable()) {
        <h1 [class]="titleClass()">
          {{ title() }}
        </h1>
      }
      @if (count() !== null && count() !== undefined) {
        <span
          [class]="countClass"
          i18n-aria-label="
            Accessible label for the badge showing how many items the page lists
          "
          aria-label="Total count">
          {{ count() }}
        </span>
      }
      @if (titleEditable()) {
        <app-inline-edit-input
          [class]="titleClass() + ' cursor-pointer'"
          activeBorder="true"
          [value]="title()"
          [size]="title()?.length"
          (submitted)="titleSubmitted.emit($event)"></app-inline-edit-input>
      }

      <div [class]="suffixClass">
        <ng-content select="[titleSuffix]" />
      </div>

      <div [class]="contentClass">
        <ng-content />
      </div>
    </div>
  `,
  imports: [InlineEditInputComponent],
})
export class PageHeaderTitleComponent {
  readonly title = input<string | null>();
  readonly titleEditable = input(false);
  readonly count = input<number | null>();
  readonly logoUrl = input<string | null>(null);

  readonly compact = input(false, { transform: booleanAttribute });

  readonly titleSubmitted = output<string>();

  protected readonly titleClass = computed(() => {
    const base = 'page-header-title font-overpass m-0 text-[22px] font-normal';

    return this.compact() ? `${base} truncate` : base;
  });

  protected readonly countClass =
    'bg-foreground/10 text-foreground/70 ml-3 inline-flex h-[22px] min-w-[22px] shrink-0 items-center justify-center rounded-full px-2 text-[13px] font-medium tabular-nums';

  protected readonly suffixClass =
    'flex shrink-0 flex-row items-center gap-1 empty:hidden';

  protected readonly contentClass = 'ml-3 flex flex-row items-center gap-3';
}
