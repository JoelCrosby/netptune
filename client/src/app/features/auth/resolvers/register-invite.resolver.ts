import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { clearUserInfo } from '@core/auth/store/auth.actions';
import { WorkspaceInvite } from '@core/auth/store/auth.models';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Injectable()
export class RegisterInviteResolver
  implements Resolve<Observable<WorkspaceInvite>> {
  constructor(private store: Store, private http: HttpClient) {}

  resolve(route: ActivatedRouteSnapshot) {
    this.store.dispatch(clearUserInfo());

    const code = route.queryParamMap.get('code');

    if (!code) return of({ success: false });

    return this.http
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
}
