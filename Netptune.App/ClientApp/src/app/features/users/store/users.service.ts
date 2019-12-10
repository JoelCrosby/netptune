import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { environment } from '@env/environment';

@Injectable()
export class UsersService {
  constructor(private http: HttpClient) {}

  getUsersInWorkspace(workspaceId: number) {
    return this.http.get<AppUser[]>(
      environment.apiEndpoint + `api/users?workspaceId=${workspaceId}`
    );
  }
}
