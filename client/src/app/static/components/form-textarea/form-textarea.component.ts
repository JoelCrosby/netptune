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

@Component({
  selector: 'app-form-textarea',
  templateUrl: './form-textarea.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideDynamicIcon],
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

  readonly input = viewChild.required<ElementRef>('input');
  readonly submitted = output<string>();

  onInputchange(event: Event) {
    const target = event.target as HTMLInputElement;
    this.value.set(target.value);
  }
}
