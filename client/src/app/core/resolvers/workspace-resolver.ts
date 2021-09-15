import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class WorkspaceResolver implements Resolve<Workspace> {
  constructor(private http: HttpClient, private store: Store) {}

  resolve(route: ActivatedRouteSnapshot): Observable<Workspace> {
    return this.get(route.paramMap.get('workspace'));
  }

  get(workspaceKey: string | null): Observable<Workspace> {
    // TODO: handle when workspaceKey is null

    return this.http
      .get<Workspace>(
        environment.apiEndpoint + `api/workspaces/${workspaceKey}`
      )
      .pipe(
        tap((workspace) => {
          this.store.dispatch(selectWorkspace({ workspace }));
        })
      );
  }
}
