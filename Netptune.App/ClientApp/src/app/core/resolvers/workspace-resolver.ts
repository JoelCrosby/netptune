import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  Resolve,
  RouterStateSnapshot,
} from '@angular/router';
import { AppState } from '@core/core.state';
import { Workspace } from '@core/models/workspace';
import { selectWorkspace } from '@core/workspaces/workspaces.actions';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class WorkspaceResolver implements Resolve<Workspace> {
  constructor(private http: HttpClient, private store: Store<AppState>) {}

  resolve(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<Workspace> {
    console.log(route);

    return this.get(route.paramMap.get('workspace'));
  }

  get(workspaceSlug: string) {
    const params = new HttpParams().append('slug', workspaceSlug);
    return this.http
      .get<Workspace>(environment.apiEndpoint + 'api/workspaces', { params })
      .pipe(tap(() => this.store.dispatch(selectWorkspace({ workspaceSlug }))));
  }
}
