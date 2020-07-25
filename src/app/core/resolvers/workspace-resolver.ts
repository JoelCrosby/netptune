import { loadProjects } from '@core/store/projects/projects.actions';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { AppState } from '@core/core.state';
import { Workspace } from '@core/models/workspace';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { environment } from '@env/environment';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class WorkspaceResolver implements Resolve<Workspace> {
  constructor(private http: HttpClient, private store: Store<AppState>) {}

  resolve(route: ActivatedRouteSnapshot): Observable<Workspace> {
    return this.get(route.paramMap.get('workspace'));
  }

  get(workspaceSlug: string): Observable<Workspace> {
    return this.http
      .get<Workspace>(
        environment.apiEndpoint + `api/workspaces/${workspaceSlug}`
      )
      .pipe(
        tap((workspace) => {
          this.store.dispatch(selectWorkspace({ workspace }));
        })
      );
  }
}
