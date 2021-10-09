import { AppUser } from '@core/models/appuser';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { ClientResponse } from '@core/models/client-response';
import { UploadResponse } from '@core/models/upload-result';

@Injectable()
export class ProfileService {
  constructor(private http: HttpClient) {}

  get(userId: string) {
    return this.http.get<AppUser>(
      environment.apiEndpoint + `api/users/${userId}`
    );
  }

  post(user: AppUser) {
    return this.http.post<AppUser>(
      environment.apiEndpoint + `api/users/${user.id}`,
      user
    );
  }

  put(user: Partial<AppUser> & { id: string }) {
    return this.http.put<AppUser>(
      environment.apiEndpoint + `api/users/${user.id}`,
      user
    );
  }

  changePassword(request: ChangePasswordRequest) {
    return this.http.patch<ClientResponse>(
      environment.apiEndpoint + 'api/auth/change-password',
      request
    );
  }

  uloadProfilePicture(data: FormData) {
    return this.http.post<ClientResponse<UploadResponse>>(
      environment.apiEndpoint + 'api/storage/profile-picture',
      data
    );
  }
}
