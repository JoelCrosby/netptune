import { Directive, input } from '@angular/core';

@Directive({
  selector: '[app-dialog-actions], app-dialog-actions, [dialogActions]',
  host: {
    class: 'dialog-actions',
    '[class.dialog-actions-align-center]': 'align() === "center"',
    '[class.dialog-actions-align-end]': 'align() === "end"',
  },
})
export class DialogActionsDirective {
  readonly align = input<('start' | 'center' | 'end') | undefined>('start');
}
