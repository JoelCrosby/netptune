import { Directive, input } from '@angular/core';

@Directive({
    selector: '[app-dialog-actions], app-dialog-actions, [dialogActions]',
    // eslint-disable-next-line @angular-eslint/no-host-metadata-property
    host: {
        class: 'dialog-actions',
        '[class.dialog-actions-align-center]': 'align() === "center"',
        '[class.dialog-actions-align-end]': 'align() === "end"',
    }
})
export class DialogActionsDirective {
  readonly align = input<('start' | 'center' | 'end') | undefined>('start');
}
