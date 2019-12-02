import { HttpClient, HttpParams } from '@angular/common/http';
import { Workspace } from '@core/models/workspace';
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { environment } from '@env/environment';

@Injectable({ providedIn: 'root' })
export class WorkspaceResolver implements Resolve<Workspace> {
  constructor(private http: HttpClient) {}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<Workspace> {
    console.log(route);

    return this.get(route.paramMap.get('workspace'));
  }

  get(workspaceSlug: string) {
    const params = new HttpParams().append('slug', workspaceSlug);
    return this.http.get<Workspace>(environment.apiEndpoint + 'api/workspaces', { params });
  }
}
