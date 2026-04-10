import { Injectable } from '@angular/core';
import { FormSelectTagsComponent } from './form-select-tags.component';

@Injectable()
export class FormSelectTagsService<TValue> {
  private select?: FormSelectTagsComponent<TValue>;

  register(select: FormSelectTagsComponent<TValue>) {
    this.select = select;
  }

  getSelect(): FormSelectTagsComponent<TValue> | undefined {
    return this.select;
  }
}
