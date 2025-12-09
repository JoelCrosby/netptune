import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DocumentService } from '@static/services/document.service';
import { Subject } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-inline-edit-input',
  templateUrl: './inline-edit-input.component.html',
  styleUrls: ['./inline-edit-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ReactiveFormsModule],
  host: {
    '[class.edit-active]': 'isEditActive()',
  },
})
export class InlineEditInputComponent implements OnInit, OnDestroy {
  private elementRef = inject(ElementRef);
  private cd = inject(ChangeDetectorRef);
  private document = inject(DocumentService);

  readonly value = input<string | null>();
  readonly size = input<number>();
  readonly activeBorder = input<boolean | string | null>();

  readonly input = viewChild.required<ElementRef>('input');
  readonly submitted = output<string>();

  control = new FormControl('', {
    updateOn: 'blur',
  });

  isEditActive = signal(false);
  onDestroy$ = new Subject<void>();

  constructor() {
    effect(() =>
      this.control.setValue(this.value() ?? '', { emitEvent: false })
    );
  }

  ngOnInit() {
    this.document.documentClicked().subscribe({
      next: this.handleDocumentClick.bind(this),
    });

    this.control.valueChanges
      .pipe(
        takeUntil(this.onDestroy$),
        tap((value) => this.onSubmit(value as string))
      )
      .subscribe();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  handleDocumentClick(target: EventTarget) {
    if (this.isEditActive()) {
      if (!this.elementRef.nativeElement.contains(target)) {
        return this.isEditActive.set(false);
      }
    } else {
      if (this.elementRef.nativeElement.contains(target)) {
        this.isEditActive.set(true);
        this.focusInput();
      }
    }
  }

  focusInput() {
    this.cd.detectChanges();

    const inputEl = this.input();
    if (inputEl) {
      this.control.setValue(this.value() as string, {
        emitEvent: false,
      });
      inputEl?.nativeElement.focus();
    }
  }

  onSubmit(value: string) {
    this.submitted.emit(value);
    this.isEditActive.set(false);
  }
}
