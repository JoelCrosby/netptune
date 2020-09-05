import { FormControl, FormGroupDirective, NgForm } from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';

export class FormErrorStateMatcher implements ErrorStateMatcher {
  isErrorState(
    control: FormControl | null,
    _: FormGroupDirective | NgForm | null
  ): boolean {
    const invalidCtrl = !!(
      control &&
      control.invalid &&
      control.touched &&
      control.parent.dirty
    );
    const invalidParent = !!(
      control &&
      control.parent &&
      control.parent.touched &&
      control.parent.invalid &&
      control.parent.dirty
    );

    return invalidCtrl || (invalidParent && control.touched);
  }
}
