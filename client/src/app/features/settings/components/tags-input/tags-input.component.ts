import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  model,
  output,
  viewChild,
} from '@angular/core';
import { DocumentService } from '@static/services/document.service';

@Component({
  selector: 'app-tags-input',
  templateUrl: './tags-input.component.html',
  styleUrls: ['./tags-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
})
export class TagsInputComponent implements AfterViewInit {
  readonly value = model<string | null>(null);
  readonly input = viewChild.required<ElementRef>('input');
  readonly document = inject(DocumentService);

  readonly submitted = output<string>();
  readonly canceled = output();

  constructor() {
    this.document.documentClicked().subscribe({
      next: this.handleDocumentClick.bind(this),
    });

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

  handleDocumentClick(target: EventTarget) {
    if (!this.input().nativeElement.contains(target)) {
      this.canceled.emit();
    }
  }
}
