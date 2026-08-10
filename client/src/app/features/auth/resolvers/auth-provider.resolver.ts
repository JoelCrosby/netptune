import { inject } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { LoginResponse } from '@core/models/session';

export const authProvider: ResolveFn<boolean> = async (
  route: ActivatedRouteSnapshot
): Promise<boolean> => {
  const expiresValue = route.queryParamMap.get('expires');
  const email = route.queryParamMap.get('email');
  const userId = route.queryParamMap.get('userId');

  if (!expiresValue || !email || !userId) {
    console.error('auth redirect query params failed: ', {
      expiresValue,
      email,
      userId,
    });

    return false;
  }

  const expires = new Date(expiresValue);

  if (Number.isNaN(expires.getTime())) {
    console.error('auth redirect expires date time failed: ', {
      expires,
    });

    return false;
  }

  const displayName = route.queryParamMap.get('displayName') ?? '';
  const pictureUrl = route.queryParamMap.get('pictureUrl') ?? '';

  const user: LoginResponse = {
    userId,
    email,
    expires: expiresValue,
    displayName,
    pictureUrl,
  };

  inject(SessionService).establish(user);
  inject(WorkspaceListService).reload();

  return true;
};
