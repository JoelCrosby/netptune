import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { Workspace } from '@core/models/workspace';
import { AppUser } from '@core/models/appuser';
import { throwError } from 'rxjs';

@Injectable()
export class UsersService {
  constructor(private http: HttpClient) {}

  get(workspace: Workspace) {
    if (!workspace) {
      return throwError('no current workspace');
    }
    return this.http.get<AppUser[]>(
      environment.apiEndpoint + `api/users?workspaceId=${workspace.id}`
    );
  }
}
