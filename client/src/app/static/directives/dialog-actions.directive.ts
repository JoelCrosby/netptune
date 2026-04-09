import { Directive, input } from '@angular/core';

@Directive({
  selector: '[app-dialog-actions], app-dialog-actions, [dialogActions]',
  host: {
    class: 'flex gap-3 mt-7',
    '[class.justify-center]': 'align() === "center"',
    '[class.justify-end]': 'align() === "end"',
  },
})
export class DialogActionsDirective {
  readonly align = input<('start' | 'center' | 'end') | undefined>('start');
}
