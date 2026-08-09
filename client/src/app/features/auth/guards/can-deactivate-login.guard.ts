import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { AuthCommandsService } from '@core/services/auth-commands.service';

export const canDeactivateLogin: CanDeactivateFn<boolean> = () => {
  inject(AuthCommandsService).clearLoginError();

  return true;
};
