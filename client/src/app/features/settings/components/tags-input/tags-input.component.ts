import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  model,
  output,
  viewChild,
} from '@angular/core';

@Component({
  selector: 'app-tags-input',
  templateUrl: './tags-input.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
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
