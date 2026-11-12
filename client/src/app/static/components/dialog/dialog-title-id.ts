import { _IdGenerator } from '@angular/cdk/a11y';
import { CdkDialogContainer, DialogRef } from '@angular/cdk/dialog';
import { effect, inject } from '@angular/core';

// Names the surrounding dialog after the heading carrying the returned id, for as
// long as `present` holds.
export function dialogTitleId(present: () => boolean = () => true): string {
  const id = inject(_IdGenerator).getId('app-dialog-title-');
  const container = inject(DialogRef, { optional: true })?.containerInstance;

  if (!(container instanceof CdkDialogContainer)) return id;

  effect((onCleanup) => {
    if (!present()) return;

    container._addAriaLabelledBy(id);
    onCleanup(() => container._removeAriaLabelledBy(id));
  });

  return id;
}
