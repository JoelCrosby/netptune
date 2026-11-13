import { Component, computed, input, output } from '@angular/core';
import { LucideColumns3 } from '@lucide/angular';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';

@Component({
  selector: 'app-board-group-hidden-notice',
  imports: [FilterActionButtonComponent],
  template: `
    <app-filter-action-button
      i18n-label="Button that opens the dialog for showing and hiding columns"
      label="Show or hide columns"
      [icon]="lucideColumns3"
      [color]="count() ? 'primary' : undefined"
      [value]="summary()"
      [count]="count()"
      [clearable]="count() > 0"
      [clearLabel]="showAllLabel"
      (cleared)="showAll.emit()"
      (action)="manage.emit()" />
  `,
})
export class BoardGroupHiddenNoticeComponent {
  readonly count = input.required<number>();

  readonly manage = output();
  readonly showAll = output();

  readonly lucideColumns3 = LucideColumns3;

  protected readonly showAllLabel = $localize`:Accessible label for the button that brings every hidden board column back:Show all columns`;

  protected readonly summary = computed(() => {
    const count = this.count();

    if (!count) {
      return $localize`:Label on a control that shows and hides columns:Columns`;
    }

    return $localize`:Shown on the columns control when some board columns are hidden. COUNT is how many of them are hidden:${count}:COUNT: hidden`;
  });
}
