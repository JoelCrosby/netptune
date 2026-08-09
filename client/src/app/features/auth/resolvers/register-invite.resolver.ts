import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { WorkspaceInvite } from '@core/models/session';
import { of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

export const registerInvite: ResolveFn<WorkspaceInvite> = (
  route: ActivatedRouteSnapshot
) => {
  {
    const http = inject(HttpClient);

    inject(AuthCommandsService).endSession();

    const code = route.queryParamMap.get('code');

    if (!code) return of({ success: false });

    return http
      .get<WorkspaceInvite>('api/auth/validate-workspace-invite', {
        params: {
          code,
        },
      })
      .pipe(
        map((res) => ({ ...res, code, success: true })),
        catchError(() => of({ success: false }))
      );
  }
};
