import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { AccordionRowComponent } from '@static/components/accordion/accordion-row.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';

// The folding "Linked tasks" band of a task's accordion. The list itself is
// projected, and stays mounted while folded.
@Component({
  selector: 'app-task-links-section',
  imports: [AccordionRowComponent, InlineButtonComponent],
  host: { class: 'block' },
  template: `
    <app-accordion-row
      [label]="label"
      [summary]="summary()"
      [last]="last()"
      [expanded]="expanded()"
      (toggled)="toggled.emit()">
      @if (canLink()) {
        <button
          type="button"
          app-inline-button
          appearance="soft"
          class="shrink-0 font-medium"
          [disabled]="disabled()"
          (click)="linkRequested.emit()">
          <span i18n="Button that links this task to another">Link task</span>
        </button>
      }
    </app-accordion-row>

    <div class="pt-1 pb-3" [class.hidden]="!expanded()">
      <ng-content />
    </div>
  `,
})
export class TaskLinksSectionComponent {
  readonly count = input(0);
  readonly expanded = input(false, { transform: booleanAttribute });
  readonly last = input(false, { transform: booleanAttribute });
  readonly canLink = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });

  readonly toggled = output();
  readonly linkRequested = output();

  protected readonly label = $localize`:Section heading for links between tasks:Linked tasks`;

  protected readonly summary = computed(() => {
    const count = this.count();

    if (!count) {
      return $localize`:Accordion summary when a task links to nothing:None yet`;
    }

    if (count === 1) {
      return $localize`:Accordion summary when a task links to one other:1 linked task`;
    }

    return $localize`:Accordion summary counting the tasks this one links to. COUNT is how many:${count}:COUNT: linked tasks`;
  });
}
