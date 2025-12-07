import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { FormErrorComponent } from './form-error.component';

@Component({
  selector: 'app-form-errors',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormErrorComponent],
  template: `
    @if (field(); as field) {
      @if (field().touched()) {
        @for (error of field().errors(); track error.kind) {
          <app-form-error>
            {{ error.message }}
          </app-form-error>
        }
      }
    }
  `,
})
export class FormErrorsComponent<TValue extends string | number> {
  field = input.required<FieldTree<TValue, TValue>>();
}
