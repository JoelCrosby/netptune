import { computed, Directive, input, model } from '@angular/core';
import {
  DisabledReason,
  FormValueControl,
  ValidationError,
  WithOptionalFieldTree,
} from '@angular/forms/signals';
import { describedByIds, errorIdFor, hintIdFor } from './form-control-a11y';

@Directive()
export class AbstractFormValueControl implements FormValueControl<string> {
  readonly name = input<string>('');
  readonly value = model('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly disabledReasons = input<
    readonly WithOptionalFieldTree<DisabledReason>[]
  >([]);
  readonly isReadonly = input<boolean>(false);
  readonly hidden = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);

  readonly showErrors = computed(
    () => this.touched() && this.errors().length > 0
  );
  readonly hintId = computed(() => hintIdFor(this.name()));
  readonly errorId = computed(() => errorIdFor(this.name()));
  readonly ariaInvalid = computed(() => (this.showErrors() ? 'true' : null));

  describedBy(hasHint: boolean): string | null {
    return describedByIds(this.name(), hasHint, this.showErrors());
  }
}
