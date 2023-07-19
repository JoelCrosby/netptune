import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { clearUserInfo } from '@core/auth/store/auth.actions';
import { WorkspaceInvite } from '@core/auth/store/auth.models';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

export const registerInvite: ResolveFn<WorkspaceInvite> = (
  route: ActivatedRouteSnapshot
) => {
  {
    const store = inject(Store);
    const http = inject(HttpClient);

    store.dispatch(clearUserInfo());

    const code = route.queryParamMap.get('code');

    if (!code) return of({ success: false });

    return http
      .get<WorkspaceInvite>(
        environment.apiEndpoint + 'api/auth/validate-workspace-invite',
        {
          params: {
            code,
          },
        }
      )
      .pipe(
        map((res) => ({ ...res, code, success: true })),
        catchError(() => of({ success: false }))
      );
  }
};
