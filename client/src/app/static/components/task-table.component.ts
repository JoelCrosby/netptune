import {
  Component,
  Signal,
  booleanAttribute,
  computed,
  contentChild,
  contentChildren,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { Params } from '@angular/router';
import { TaskColumnRow } from '@core/tasks/task-columns';
import { ProjectTasksHubService } from '@core/services/tasks-hub.service';
import { DatatableCellTemplateDirective } from './datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from './datatable/datatable-empty.directive';
import { DatatableComponent } from './datatable/datatable.component';
import {
  DatatableColumn,
  DatatableDataSource,
  DatatableMenuItem,
} from './datatable/datatable.types';

const emptyParams: Signal<Params> = computed(() => ({}));

@Component({
  selector: 'app-task-table',
  imports: [DatatableComponent],
  host: { class: 'flex min-h-0 flex-col', '[class.flex-1]': 'fill()' },
  template: `
    <app-datatable
      [data]="data()"
      [selection]="selection()"
      [customizableColumns]="customizableColumns()"
      [stickyHeader]="stickyHeader()"
      [fill]="fill()"
      [rounded]="rounded()"
      [containerClass]="containerClass()"
      [tableClass]="tableClass()"
      [headerClass]="headerClass()"
      [emptyCellClass]="emptyCellClass()"
      [emptyMessage]="emptyMessage()"
      [errorMessage]="errorMessage()"
      [itemLabel]="itemLabel()"
      [skeletonRows]="skeletonRows()"
      [projectedCellTemplates]="cellTemplates()"
      [projectedEmptyTemplate]="emptyState()?.templateRef ?? null"
      (selectionChanged)="selectionChanged.emit($event)"
      (loaded)="loaded.emit($event)" />
  `,
})
export class TaskTableComponent<T extends TaskColumnRow> {
  private readonly hub = inject(ProjectTasksHubService);

  private readonly datatable = viewChild.required(DatatableComponent<T>);

  readonly key = input.required<string>();
  readonly columns = input.required<readonly DatatableColumn<T>[]>();

  readonly url = input<string>('');
  readonly params = input<Signal<Params>>(emptyParams);
  readonly menu = input<readonly DatatableMenuItem<T>[]>([]);
  readonly reloadSignal = input<Signal<unknown> | null>(null);

  readonly items = input<Signal<readonly T[]> | null>(null);
  readonly loading = input<Signal<boolean> | null>(null);

  readonly selection = input(false, { transform: booleanAttribute });
  readonly customizableColumns = input(false, { transform: booleanAttribute });
  readonly stickyHeader = input(false, { transform: booleanAttribute });
  readonly fill = input(false, { transform: booleanAttribute });
  readonly rounded = input(true, { transform: booleanAttribute });
  readonly containerClass = input('');
  readonly tableClass = input('');
  readonly headerClass = input('');
  readonly emptyCellClass = input('');
  readonly emptyMessage = input(
    $localize`:Empty state for a task table:No tasks to display.`
  );
  readonly errorMessage = input(
    $localize`:Shown when a task table fails to load:Tasks could not be loaded.`
  );
  readonly itemLabel = input(
    $localize`:Plural noun for tasks, used in the row summary:tasks`
  );
  readonly skeletonRows = input(8);

  readonly selectionChanged = output<T[]>();
  readonly loaded = output<{ totalCount: number; hasValue: boolean }>();

  protected readonly cellTemplates = contentChildren<
    DatatableCellTemplateDirective<T>
  >(DatatableCellTemplateDirective, { descendants: true });

  protected readonly emptyState = contentChild(DatatableEmptyDirective, {
    descendants: true,
  });

  protected readonly data = computed<DatatableDataSource<T>>(() => {
    const items = this.items();

    if (items) {
      return {
        key: this.key(),
        columns: this.columns(),
        trackBy: (_: number, task: T) => task.id,
        menu: this.menu(),
        items,
        loading: this.loading() ?? undefined,
      };
    }

    return {
      key: this.key(),
      columns: this.columns(),
      trackBy: (_: number, task: T) => task.id,
      menu: this.menu(),
      resource: {
        url: this.url(),
        params: this.params(),
      },
      rows: (response) => response?.payload?.items ?? [],
      reloadSignal: this.reloadSignal() ?? this.hub.updateVersion,
    };
  });

  clearSelection() {
    this.datatable().clearSelection();
  }

  reload() {
    this.datatable().reload();
  }
}
