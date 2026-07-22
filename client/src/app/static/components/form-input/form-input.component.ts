import { Component, ElementRef, input, output, viewChild } from '@angular/core';

import { LucideDynamicIcon, LucideIconInput } from '@lucide/angular';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import { FormControlFieldComponent } from '../form-control/form-control-field.component';
import {
  FormControlHintDirective,
  FormControlInputDirective,
  FormControlLabelDirective,
  FormControlPrefixDirective,
} from '../form-control/form-control.directives';
import { FormErrorComponent } from '../form-error/form-error.component';
import { DatePickerComponent } from '../date-picker/date-picker.component';

@Component({
  selector: 'app-form-input',
  imports: [
    LucideDynamicIcon,
    FormErrorComponent,
    FormControlFieldComponent,
    FormControlInputDirective,
    FormControlLabelDirective,
    FormControlHintDirective,
    FormControlPrefixDirective,
    DatePickerComponent,
  ],
  template: `<div
    class="nept-form-control mb-[1.4rem] w-[inherit]"
    [class.mb-0!]="noMargin()">
    @if (label()) {
      <label [for]="name()" appFormLabel>
        {{ label() }}
      </label>
    }

    <app-form-control-field
      [invalid]="touched() && invalid()"
      [active]="pending()">
      @if (prefix()) {
        <div appFormPrefix>{{ prefix() }}</div>
      }

      @if (type() === 'date') {
        <app-date-picker
          class="min-w-0 flex-1"
          appearance="bare"
          [controlId]="name()"
          [value]="value()"
          [placeholder]="placeholder() || 'Select date'"
          [ariaLabel]="label() || 'Choose date'"
          [min]="min()"
          [max]="max()"
          [disabled]="disabled()"
          [required]="required()"
          [buttonClass]="prefix() ? 'px-0 pr-3' : 'px-3'"
          (valueChange)="value.set($event)"
          (touched)="touched.set(true)" />
      } @else {
        <input
          #input
          appFormInput
          [id]="name()"
          [value]="value()"
          [disabled]="disabled()"
          [required]="required()"
          [attr.minLength]="minLength()"
          [attr.maxLength]="maxLength()"
          [attr.type]="type()"
          [attr.autocomplete]="autocomplete()"
          [attr.placeholder]="placeholder()"
          [style.padding]="prefix() ? '0 .8rem 0 0' : '0 .8rem'"
          (input)="onInputchange($event)"
          (blur)="touched.set(true)" />
      }

      @if (icon()) {
        <svg
          class="mr-3"
          [lucideIcon]="icon()!"
          size="20"
          aria-hidden="true"></svg>
      }
    </app-form-control-field>

    @if (hint()) {
      <small appFormHint> {{ hint() }} </small>
    }

    @if (touched() && errors().length > 0) {
      @for (error of errors(); track error.kind) {
        <app-form-error>
          {{ error.message }}
        </app-form-error>
      }
    }

    <div class="mt-[.4rem]">
      <ng-content />
    </div>
  </div> `,
})
export class FormInputComponent extends AbstractFormValueControl {
  readonly label = input<string>();
  readonly icon = input<LucideIconInput | null>();
  readonly prefix = input<string | null>();
  readonly autocomplete = input('off');
  readonly placeholder = input<string | null>();
  readonly hint = input<string | null>();
  readonly min = input<string>();
  readonly max = input<string>();
  readonly minLength = input<string | number | null>();
  readonly maxLength = input<string | number | null>();
  readonly loading = input<boolean | null>(false);
  readonly type = input<'text' | 'number' | 'email' | 'password' | 'date'>(
    'text'
  );
  readonly pending = input(false);
  readonly noMargin = input(false);

  readonly input = viewChild<ElementRef>('input');

  readonly submitted = output<string>();

  onInputchange(event: Event) {
    const target = event.target as HTMLInputElement;
    const value = target.value;

    this.value.set(value);
  }
}
