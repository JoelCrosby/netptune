import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { fromEvent, Subject } from 'rxjs';
import { takeUntil, tap, throttleTime } from 'rxjs/operators';

@Component({
    selector: 'app-tags-input',
    templateUrl: './tags-input.component.html',
    styleUrls: ['./tags-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormsModule, ReactiveFormsModule]
})
export class TagsInputComponent implements OnInit, OnDestroy, AfterViewInit {
  @Input() value: string | null = null;
  @ViewChild('input') input!: ElementRef;

  @Output() submitted = new EventEmitter<string>();
  @Output() canceled = new EventEmitter();

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

    this.formControl.setValue(this.value, { emitEvent: false });
  }

  ngAfterViewInit() {
    this.input.nativeElement.focus();
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
    if (!this.input.nativeElement.contains(event.target)) {
      this.canceled.emit();
    }
  }
}
