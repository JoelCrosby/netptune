import { Directive, computed, input } from '@angular/core';
import { cn } from '@static/components/button/button.variants';
import { fieldLabelClass } from '@static/components/field-row/field-row.component';

// The document layout widens the label column, so every field row takes its width.
@Directive()
export abstract class TaskFieldBase {
  readonly labelWidth = input('w-24');
  readonly disabled = input(false);

  protected readonly labelClass = computed(() => {
    return cn(fieldLabelClass, this.labelWidth());
  });
}
