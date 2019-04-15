import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { Workspace } from '@app/core/models/workspace';
import { AppUser } from '@app/core/models/appuser';

@Injectable()
export class UsersService {
  constructor(private http: HttpClient) {}

  get(workspace: Workspace) {
    return this.http.get<AppUser[]>(
      environment.apiEndpoint + `api/users?workspaceId=${workspace.id}`
    );
  }
}
