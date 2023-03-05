import { Injectable } from '@angular/core';
import { FormSelectComponent } from './form-select.component';

@Injectable()
export class FormSelectService {
  private select?: FormSelectComponent;

  register(select: FormSelectComponent) {
    this.select = select;
  }

  getSelect(): FormSelectComponent | undefined {
    return this.select;
  }
}
