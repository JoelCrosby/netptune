import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { ClientResponse } from '@core/models/client-response';
import { WorkspaceRole } from '@core/enums/workspace-role';
import {
  appendPageParams,
  DEFAULT_PAGE_SIZE,
  Page,
  PageQuery,
} from '@core/models/pagination';

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private http = inject(HttpClient);

  getUser(userId: string) {
    return this.http.get<WorkspaceAppUser>(`api/users/${userId}`);
  }

  getUsersInWorkspace(query?: PageQuery) {
    return this.http.get<ClientResponse<Page<WorkspaceAppUser>>>(`api/users`, {
      params: appendPageParams(new HttpParams(), {
        page: query?.page ?? 1,
        pageSize: query?.pageSize ?? DEFAULT_PAGE_SIZE,
      }),
    });
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

  resendInvite(email: string) {
    return this.http.post<ClientResponse>(`api/users/resend-invite`, {
      emailAddresses: [email],
    });
  }

  toggleUserPermission(userId: string, permission: string) {
    return this.http.post<ClientResponse>(`api/users/toggle-permission`, {
      userId,
      permission,
    });
  }

  updateWorkspaceRole(userId: string, role: WorkspaceRole) {
    return this.http.put<
      ClientResponse<{ role: WorkspaceRole; permissions: string[] }>
    >(`api/users/role`, { userId, role });
  }
}
