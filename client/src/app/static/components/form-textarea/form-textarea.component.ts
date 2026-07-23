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

@Component({
  selector: 'app-form-textarea',
  imports: [
    LucideDynamicIcon,
    FormControlFieldComponent,
    FormControlInputDirective,
    FormControlLabelDirective,
    FormControlHintDirective,
    FormControlPrefixDirective,
    FormErrorComponent,
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
      [active]="!!value() && touched()">
      @if (prefix()) {
        <div appFormPrefix>{{ prefix() }}</div>
      }

      <textarea
        #input
        appFormInput
        class="leading-[1.6rem]!"
        [id]="name()"
        [value]="value()"
        [disabled]="disabled()"
        [required]="required()"
        [attr.maxLength]="maxLength()"
        [attr.minLength]="minLength()"
        [attr.placeholder]="placeholder()"
        [style.padding]="prefix() ? '.6rem .8rem 1rem 0' : '.6rem .8rem'"
        [rows]="rows()"
        (input)="onInputchange($event)"
        (blur)="touched.set(true)"></textarea>

      @if (icon()) {
        <svg [lucideIcon]="icon()!" size="20" aria-hidden="true"></svg>
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
  </div> `,
})
export class FormTextAreaComponent extends AbstractFormValueControl {
  readonly label = input<string>();
  readonly icon = input<LucideIconInput | null>();
  readonly prefix = input<string>();
  readonly placeholder = input<string | null>(null);
  readonly hint = input<string | null>(null);
  readonly minLength = input<string | number | undefined | null>(null);
  readonly maxLength = input<string | number | undefined | null>(null);
  readonly rows = input('2');
  readonly noMargin = input(false);

  readonly input = viewChild.required<ElementRef>('input');
  readonly submitted = output<string>();

  onInputchange(event: Event) {
    const target = event.target as HTMLInputElement;
    this.value.set(target.value);
  }
}
