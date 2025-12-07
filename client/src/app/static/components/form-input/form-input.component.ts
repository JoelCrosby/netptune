import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { NgControl } from '@angular/forms';

import { MatIcon } from '@angular/material/icon';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import { FormErrorComponent } from '../form-error/form-error.component';

@Component({
  selector: 'app-form-input',
  templateUrl: './form-input.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIcon, FormErrorComponent],
})
export class FormInputComponent extends AbstractFormValueControl {
  readonly ngControl = inject(NgControl, { self: true, optional: true });
  readonly formControl = input<NgControl>();

  readonly label = input<string>();
  readonly icon = input<string | null>();
  readonly prefix = input<string | null>();
  readonly autocomplete = input('off');
  readonly placeholder = input<string | null>();
  readonly hint = input<string | null>();
  readonly loading = input<boolean | null>(false);
  readonly type = input<'text' | 'number' | 'email' | 'password'>('text');
  readonly pending = input(false);

  readonly input = viewChild.required<ElementRef>('input');

  readonly submitted = output<string>();

  constructor() {
    super();
  }

  onInputchange(event: Event) {
    const target = event.target as HTMLInputElement;
    const value = target.value;

    this.value.set(value);
  }
}
