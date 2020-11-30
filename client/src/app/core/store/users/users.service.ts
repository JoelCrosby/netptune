import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  constructor(private http: HttpClient) {}

  getUsersInWorkspace() {
    return this.http.get<AppUser[]>(environment.apiEndpoint + `api/users`);
  }

  inviteUsersToWorkspace(emailAddresses: string[]) {
    return this.http.post<AppUser[]>(
      environment.apiEndpoint + `api/users/invite`,
      {
        emailAddresses,
      }
    );
  }

  removeUsersFromWorkspace(emailAddresses: string[]) {
    return this.http.post<AppUser[]>(
      environment.apiEndpoint + `api/users/remove`,
      {
        emailAddresses,
      }
    );
  }
}
