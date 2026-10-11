import { DialogRef } from '@angular/cdk/dialog';
import {
  afterNextRender,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { savedQueriesResource } from '@app/features/queries/resources/saved-query.resource';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import {
  FilterOption,
  FilterOptionListComponent,
} from '@static/components/filter-option-list/filter-option-list.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

// Only shared queries are offered: a private one returns not found to everyone but its owner, so
// other readers would see a broken embed.
@Component({
  selector: 'app-query-picker-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FilterOptionListComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    <app-dialog-title
      noMargin
      class="mb-3"
      i18n="Title of the dialog that picks a saved query to embed in a page">
      Embed a saved query
    </app-dialog-title>

    <app-filter-option-list
      class="w-full!"
      [open]="opened()"
      [multiple]="false"
      [dismissKeyHint]="false"
      [options]="options()"
      [listAriaLabel]="listLabel"
      [searchPlaceholder]="searchLabel"
      [emptyMessage]="emptyLabel()"
      [loading]="queries.isLoading()"
      (toggled)="onPicked($event)" />

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without acting">Cancel</span>
      </button>
    </div>
  `,
})
export class QueryPickerDialogComponent {
  static readonly width = '480px';

  private readonly dialogRef = inject<DialogRef<string>>(DialogRef);

  protected readonly opened = signal(false);
  protected readonly listLabel = $localize`:Accessible label for the list of saved queries that can be embedded:Saved queries`;
  protected readonly searchLabel = $localize`:Placeholder in the box that narrows the saved queries offered for embedding:Search saved queries`;

  protected readonly queries = savedQueriesResource();

  protected readonly emptyLabel = computed(() => {
    if (!this.queries.canRead()) {
      return $localize`:Shown in the saved query picker to someone without access to saved queries:You don't have access to saved queries`;
    }

    return $localize`:Shown when there are no shared saved queries to embed:No shared queries yet. Only shared queries can be embedded, so everyone reading the page sees the same tasks.`;
  });

  protected readonly options = computed<FilterOption<string>[]>(() => {
    const shared = this.queries.value().filter((query) => query.isShared);

    return shared.map((query) => ({
      value: query.slug,
      label: query.name,
      hint: query.description ?? undefined,
    }));
  });

  constructor() {
    afterNextRender(() => this.opened.set(true));
  }

  protected onPicked(slug: string) {
    this.dialogRef.close(slug);
  }
}
