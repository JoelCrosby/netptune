import { Directive, input, model, output } from '@angular/core';

@Directive()
export abstract class AbstractCheckableControl {
  readonly checked = model(false);
  readonly disabled = input(false);
  readonly changed = output<boolean>();

  protected onChanged(event: Event) {
    const input = event.target as HTMLInputElement;

    this.checked.set(input.checked);
    this.changed.emit(input.checked);
  }
}
