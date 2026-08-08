import { httpResource } from '@angular/common/http';
import { inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { AppUser } from '../models/appuser';
import { ClientResponse } from '../models/client-response';
import { LoginMethods } from '../models/login-methods';
import { selectCurrentUserId } from '../store/auth/auth.selectors';
import { reloadOnRefresh } from '../util/reload-on-refresh';

/* The signed-in user's own record, which no workspace permission gates. */
export const profileResource = () => {
  const store = inject(Store);
  const userId = store.selectSignal(selectCurrentUserId);

  const resource = httpResource<AppUser>(() => {
    const id = userId();

    return id ? { url: `api/users/${id}` } : undefined;
  });

  reloadOnRefresh(resource, ['profile']);

  return resource;
};

export const loginMethodsResource = () => {
  const resource = httpResource<LoginMethods>(
    () => ({ url: 'api/auth/login-methods' }),
    {
      defaultValue: { providers: [], hasPassword: false },
      parse: (response) => {
        return (
          (response as ClientResponse<LoginMethods>).payload ?? {
            providers: [],
            hasPassword: false,
          }
        );
      },
    }
  );

  reloadOnRefresh(resource, ['profile']);

  return resource;
};
