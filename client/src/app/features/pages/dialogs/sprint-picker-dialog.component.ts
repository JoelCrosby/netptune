import { DialogRef } from '@angular/cdk/dialog';
import {
  afterNextRender,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';
import { sprintResource } from '@core/resources/sprint.resource';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import {
  FilterOption,
  FilterOptionListComponent,
} from '@static/components/filter-option-list/filter-option-list.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  selector: 'app-sprint-picker-dialog',
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
      i18n="
        Title of the dialog that picks the sprint to write release notes for
      ">
      Release notes from a sprint
    </app-dialog-title>

    <app-filter-option-list
      class="w-full!"
      [open]="opened()"
      [multiple]="false"
      [dismissKeyHint]="false"
      [options]="options()"
      [listAriaLabel]="listLabel"
      [searchPlaceholder]="searchLabel"
      [emptyMessage]="emptyLabel"
      [loading]="sprints.isLoading()"
      (toggled)="onPicked($event)" />

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without acting">Cancel</span>
      </button>
    </div>
  `,
})
export class SprintPickerDialogComponent {
  static readonly width = '480px';

  private readonly dialogRef = inject<DialogRef<number>>(DialogRef);

  protected readonly opened = signal(false);
  protected readonly listLabel = $localize`:Accessible label for the list of completed sprints offered for release notes:Completed sprints`;
  protected readonly searchLabel = $localize`:Placeholder in the box that narrows the completed sprints:Search sprints`;
  protected readonly emptyLabel = $localize`:Shown when no sprint has been completed yet:No completed sprints yet`;

  protected readonly sprints = sprintResource([SprintStatus.completed]);

  protected readonly options = computed<FilterOption<number>[]>(() => {
    return this.sprints.value().map((sprint) => {
      return { value: sprint.id, label: sprint.name };
    });
  });

  constructor() {
    afterNextRender(() => this.opened.set(true));
  }

  protected onPicked(sprintId: number) {
    this.dialogRef.close(sprintId);
  }
}
