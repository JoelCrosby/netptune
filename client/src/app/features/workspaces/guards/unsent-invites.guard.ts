import { inject, Signal } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { ConfirmationService } from '@core/services/confirmation.service';

export interface HasUnsentInvites {
  unsentInviteCount: Signal<number>;
}

export const confirmLeaveWithUnsentInvites: CanDeactivateFn<
  HasUnsentInvites
> = (component) => {
  const count = component.unsentInviteCount();

  if (count === 0) return true;

  return inject(ConfirmationService).open({
    title: $localize`:Title of the dialog shown when leaving workspace setup with unsent invitations:Leave without sending invites?`,
    message:
      count === 1
        ? $localize`:Body of the dialog shown when leaving workspace setup with one unsent invitation:1 address you added has not been invited.`
        : $localize`:Body of the dialog shown when leaving workspace setup with unsent invitations. COUNT is how many:${count}:COUNT: addresses you added have not been invited.`,
    acceptLabel: $localize`:Confirms the action in a dialog:Leave`,
    cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
    color: 'warn',
  });
};
