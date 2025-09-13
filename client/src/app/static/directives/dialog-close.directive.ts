import { FocusOrigin } from '@angular/cdk/a11y';
import { DialogRef, Dialog } from '@angular/cdk/dialog';
import { Directive, OnInit, OnChanges, Input, ElementRef, SimpleChanges, inject } from '@angular/core';

function getClosestDialog<TResult, TComponent>(
  element: ElementRef<HTMLElement>,
  openDialogs: DialogRef<TResult, TComponent>[]
) {
  let parent: HTMLElement | null = element.nativeElement.parentElement;

  while (parent && !parent.classList.contains('mat-mdc-dialog-container')) {
    parent = parent.parentElement;
  }

  return parent ? openDialogs.find((dialog) => dialog.id === parent!.id) : null;
}

function closeDialogVia<R>(
  ref: DialogRef<R>,
  interactionType: FocusOrigin,
  result?: R
) {
  (
    ref as unknown as { _closeInteractionType: FocusOrigin }
  )._closeInteractionType = interactionType;
  return ref.close(result);
}

@Directive({
    selector: '[app-dialog-close], [dialogClose]',
    exportAs: 'matDialogClose',
    // eslint-disable-next-line @angular-eslint/no-host-metadata-property
    host: {
        '(click)': 'onButtonClick($event)',
        '[attr.aria-label]': 'ariaLabel || null',
        '[attr.type]': 'type',
    }
})
export class DialogCloseDirective<TResult> implements OnInit, OnChanges {
  dialogRef = inject<DialogRef<TResult>>(DialogRef, { optional: true });
  private _elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private _dialog = inject(Dialog);

  @Input('aria-label') ariaLabel?: string;
  @Input() type: 'submit' | 'button' | 'reset' = 'button';
  @Input() dialogResult?: TResult;

  ngOnInit() {
    if (!this.dialogRef) {
      this.dialogRef = getClosestDialog(
        this._elementRef,
        // eslint-disable-next-line @typescript-eslint/no-unsafe-argument, @typescript-eslint/no-explicit-any
        this._dialog.openDialogs as any
      )!;
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    const proxiedChange =
      changes['_matDialogClose'] || changes['_matDialogCloseResult'];

    if (proxiedChange) {
      this.dialogResult = proxiedChange.currentValue;
    }
  }

  onButtonClick(event: MouseEvent) {
    closeDialogVia(
      this.dialogRef,
      event.screenX === 0 && event.screenY === 0 ? 'keyboard' : 'mouse',
      this.dialogResult
    );
  }
}
