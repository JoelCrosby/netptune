import {
  UntypedFormControl,
  FormGroupDirective,
  NgForm,
  FormControl,
} from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';

export class FormErrorStateMatcher implements ErrorStateMatcher {
  isErrorState(
    control: UntypedFormControl | FormControl | null,
    _: FormGroupDirective | NgForm | null
  ): boolean {
    if (control === null) {
      return true;
    }

    const invalidCtrl = !!(
      control &&
      control.invalid &&
      control.touched &&
      control.parent?.dirty
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
