import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';

import { LucideDynamicIcon, LucideIconInput } from '@lucide/angular';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import { FormErrorComponent } from '../form-error/form-error.component';

@Component({
  selector: 'app-form-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideDynamicIcon, FormErrorComponent],
  template: `<div class="nept-form-control" [class.mb-0!]="noMargin()">
    @if (label()) {
      <label [for]="name()" class="form-control-label">
        {{ label() }}
      </label>
    }

    <div
      class="form-control-input"
      [class.invalid]="touched() && invalid()"
      [class.active]="pending()">
      @if (prefix()) {
        <div class="form-control-prefix">{{ prefix() }}</div>
      }

      <input
        #input
        [id]="name()"
        [value]="value()"
        [disabled]="disabled()"
        [class.form-control-disabled]="disabled()"
        [attr.type]="type()"
        [attr.autocomplete]="autocomplete()"
        [attr.placeholder]="placeholder()"
        [style.padding]="prefix() ? '0 .8rem 0 0' : '0 .8rem'"
        (input)="onInputchange($event)"
        (blur)="touched.set(true)" />

      @if (icon()) {
        <svg
          class="mr-3"
          [lucideIcon]="icon()!"
          size="20"
          aria-hidden="true"></svg>
      }
    </div>

    @if (hint()) {
      <small class="form-control-hint"> {{ hint() }} </small>
    }

    @if (errors()) {
      @for (error of errors(); track error.kind) {
        <app-form-error>
          {{ error.message }}
        </app-form-error>
      }
    }

    <div class="form-control-content">
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
  readonly loading = input<boolean | null>(false);
  readonly type = input<'text' | 'number' | 'email' | 'password'>('text');
  readonly pending = input(false);
  readonly noMargin = input(false);

  readonly input = viewChild.required<ElementRef>('input');

  readonly submitted = output<string>();

  onInputchange(event: Event) {
    const target = event.target as HTMLInputElement;
    const value = target.value;

    this.value.set(value);
  }
}
