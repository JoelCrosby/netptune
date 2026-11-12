import { computed, Directive, input, model } from '@angular/core';
import {
  DisabledReason,
  FormValueControl,
  ValidationError,
  WithOptionalFieldTree,
} from '@angular/forms/signals';
import {
  controlIdFor,
  describedByIds,
  errorIdFor,
  hintIdFor,
  nextControlInstanceId,
} from './form-control-a11y';

@Directive()
export class AbstractFormValueControl implements FormValueControl<string> {
  readonly name = input<string>('');
  readonly inputId = input<string | null>(null);
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

  private readonly instanceId = nextControlInstanceId();

  readonly showErrors = computed(
    () => this.touched() && this.errors().length > 0
  );
  readonly controlId = computed(() => {
    return this.inputId() || controlIdFor(this.name(), this.instanceId);
  });
  readonly hintId = computed(() => hintIdFor(this.controlId()));
  readonly errorId = computed(() => errorIdFor(this.controlId()));
  readonly ariaInvalid = computed(() => (this.showErrors() ? 'true' : null));

  describedBy(hasHint: boolean): string | null {
    return describedByIds(this.controlId(), hasHint, this.showErrors());
  }
}
