import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  input,
  output,
  viewChild,
} from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { fromEvent, Subject } from 'rxjs';
import { takeUntil, tap, throttleTime } from 'rxjs/operators';

@Component({
  selector: 'app-tags-input',
  templateUrl: './tags-input.component.html',
  styleUrls: ['./tags-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ReactiveFormsModule],
})
export class TagsInputComponent implements OnInit, OnDestroy, AfterViewInit {
  readonly value = input<string | null>(null);
  readonly input = viewChild.required<ElementRef>('input');

  readonly submitted = output<string>();
  readonly canceled = output();

  onDestroy$ = new Subject<void>();
  formControl = new FormControl<string | null>(null);

  ngOnInit() {
    fromEvent(document, 'mousedown', {
      passive: true,
    })
      .pipe(
        takeUntil(this.onDestroy$),
        throttleTime(200),
        tap(this.handleDocumentClick.bind(this))
      )
      .subscribe();

    this.formControl.setValue(this.value(), { emitEvent: false });
  }

  ngAfterViewInit() {
    this.input().nativeElement.focus();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  onSubmit() {
    const value = this.formControl.value as string;
    this.submitted.emit(value);
  }

  handleDocumentClick(event: Event) {
    if (!this.input().nativeElement.contains(event.target)) {
      this.canceled.emit();
    }
  }
}
