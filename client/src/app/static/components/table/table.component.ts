import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  input,
} from '@angular/core';

@Component({
  selector: 'app-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="containerClass()">
      <table [class]="tableClass()">
        <ng-content />
      </table>
    </div>
  `,
})
export class TableComponent {
  readonly containerClass = input('border-border overflow-auto rounded border');
  readonly tableClass = input('w-full text-sm');
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
