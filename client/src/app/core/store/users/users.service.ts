import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { ClientResponse } from '@core/models/client-response';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  constructor(private http: HttpClient) {}

  getUsersInWorkspace() {
    return this.http.get<WorkspaceAppUser[]>(
      environment.apiEndpoint + `api/users`
    );
  }

  inviteUsersToWorkspace(emailAddresses: string[]) {
    return this.http.post<ClientResponse>(
      environment.apiEndpoint + `api/users/invite`,
      {
        emailAddresses,
      }
    );
  }

  removeUsersFromWorkspace(emailAddresses: string[]) {
    return this.http.post<ClientResponse>(
      environment.apiEndpoint + `api/users/remove`,
      {
        emailAddresses,
      }
    );
  }
}
