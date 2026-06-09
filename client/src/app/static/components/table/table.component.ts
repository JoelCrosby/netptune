import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  computed,
  input,
} from '@angular/core';
import { twMerge } from 'tailwind-merge';

const defaultContainerClass = 'border-border rounded border custom-scroll';
const defaultTableClass = 'w-full text-sm custom-scroll';

@Component({
  selector: 'app-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="mergedContainerClass()">
      <table [class]="mergedTableClass()">
        <ng-content />
      </table>
    </div>
  `,
})
export class TableComponent {
  readonly containerClass = input('');
  readonly tableClass = input('');

  protected readonly mergedContainerClass = computed(() =>
    twMerge(defaultContainerClass, this.containerClass())
  );
  protected readonly mergedTableClass = computed(() =>
    twMerge(defaultTableClass, this.tableClass())
  );
}

@Directive({
  selector: 'thead[appTableHead]',
  host: {
    class: 'bg-background border-border border-b',
    '[class.sticky]': 'sticky',
    '[class.top-0]': 'sticky',
    '[class.z-10]': 'sticky',
  },
})
export class TableHeadDirective {
  readonly sticky = input(false);
}

@Directive({
  selector: 'tr[appTableHeaderRow]',
  host: {
    class: 'text-left text-xs font-medium tracking-wide uppercase',
  },
})
export class TableHeaderRowDirective {}

@Directive({
  selector: 'tr[appTableRow]',
  host: {
    class: 'border-border hover:bg-foreground/5 border-b transition-colors',
  },
})
export class TableRowDirective {}

@Directive({
  selector: 'td[appTableEmptyCell]',
  host: {
    class: 'text-muted px-4 py-10 text-center',
  },
})
export class TableEmptyCellDirective {}
