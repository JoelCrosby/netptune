import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { AccordionRowComponent } from '@static/components/accordion/accordion-row.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';

// The folding "Files" band of a task's accordion. The dropzone and file list are
// projected, and stay mounted while folded so an upload keeps running.
@Component({
  selector: 'app-task-files-section',
  imports: [AccordionRowComponent, InlineButtonComponent],
  host: { class: 'block' },
  template: `
    <app-accordion-row
      [label]="label"
      [summary]="summary()"
      [last]="last()"
      [expanded]="expanded()"
      (toggled)="toggled.emit()">
      <button
        type="button"
        app-inline-button
        appearance="soft"
        class="shrink-0 font-medium"
        (click)="chooseRequested.emit()">
        <span i18n="Button that opens the file picker">Choose files</span>
      </button>
    </app-accordion-row>

    <div class="pt-1 pb-3" [class.hidden]="!expanded()">
      <ng-content />
    </div>
  `,
})
export class TaskFilesSectionComponent {
  readonly count = input(0);
  readonly expanded = input(false, { transform: booleanAttribute });
  readonly last = input(false, { transform: booleanAttribute });

  readonly toggled = output();
  readonly chooseRequested = output();

  protected readonly label = $localize`:Section heading for files attached to a task:Files`;

  protected readonly summary = computed(() => {
    const count = this.count();

    if (!count) {
      return $localize`:Prompt on the collapsed files section:Drop a file to attach`;
    }

    if (count === 1) {
      return $localize`:Accordion summary when a task has one attachment:1 file`;
    }

    return $localize`:Accordion summary counting a task's attachments. COUNT is how many:${count}:COUNT: files`;
  });
}
