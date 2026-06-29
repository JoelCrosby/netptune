import {
  AfterViewInit,
  Component,
  ElementRef,
  model,
  output,
  viewChild,
} from '@angular/core';
import { FormControlFieldComponent } from '@app/static/components/form-control/form-control-field.component';
import { FormControlInputDirective } from '@app/static/components/form-control/form-control.directives';

@Component({
  selector: 'app-tags-input',
  imports: [FormControlFieldComponent, FormControlInputDirective],
  template: `<div class="nept-form-control mb-[1.4rem] w-[inherit]">
    <app-form-control-field>
      <input
        #input
        appFormInput
        type="text"
        [(value)]="value"
        (keydown.enter)="onSubmit($event)" />
    </app-form-control-field>
  </div> `,
})
export class TagsInputComponent implements AfterViewInit {
  readonly value = model<string | null>(null);
  readonly input = viewChild.required<ElementRef>('input');

  readonly submitted = output<string>();
  readonly canceled = output();

  constructor() {
    this.value.set(this.value());
  }

  ngAfterViewInit() {
    this.input().nativeElement.focus();
  }

  onSubmit(event: Event) {
    const input = event.target as HTMLInputElement;
    const value = input.value as string;

    this.value.set(value);

    if (value) {
      this.submitted.emit(value);
    }
  }
}
