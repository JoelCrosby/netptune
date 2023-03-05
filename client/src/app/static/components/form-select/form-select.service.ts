import { Injectable } from '@angular/core';
import { FormSelectComponent } from './form-select.component';

@Injectable()
export class FormSelectService<TValue> {
  private select?: FormSelectComponent<TValue>;

  register(select: FormSelectComponent<TValue>) {
    this.select = select;
  }

  getSelect(): FormSelectComponent<TValue> | undefined {
    return this.select;
  }
}
