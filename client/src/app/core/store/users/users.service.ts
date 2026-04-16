import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { ClientResponse } from '@core/models/client-response';

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private http = inject(HttpClient);

  getUser(userId: string) {
    return this.http.get<WorkspaceAppUser>(`api/users/${userId}`);
  }

  getUsersInWorkspace() {
    return this.http.get<WorkspaceAppUser[]>(`api/users`);
  }

  inviteUsersToWorkspace(emailAddresses: string[]) {
    return this.http.post<ClientResponse>(`api/users/invite`, {
      emailAddresses,
    });
  }

  removeUsersFromWorkspace(emailAddresses: string[]) {
    return this.http.post<ClientResponse>(`api/users/remove`, {
      emailAddresses,
    });
  }

  toggleUserPermission(userId: string, permission: string) {
    return this.http.post<ClientResponse>(`api/users/toggle-permission`, {
      userId,
      permission,
    });
  }
}
