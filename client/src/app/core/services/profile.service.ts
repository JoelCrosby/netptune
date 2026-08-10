import { AppUser } from '@core/models/appuser';
import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { ClientResponse } from '@core/models/client-response';
import { LoginMethods } from '@core/models/login-methods';
import { SetPasswordRequest } from '@core/models/requests/set-password-request';
import { UploadResponse } from '@core/models/upload-result';
import { map } from 'rxjs/operators';

@Service()
export class ProfileService {
  private http = inject(HttpClient);

  get(userId: string) {
    return this.http.get<AppUser>(`api/users/${userId}`);
  }

  put(user: Partial<AppUser> & { id: string }) {
    return this.http.put<ClientResponse<AppUser>>(`api/users/${user.id}`, user);
  }

  changePassword(request: ChangePasswordRequest) {
    return this.http.patch<ClientResponse>('api/auth/change-password', request);
  }

  setPassword(request: SetPasswordRequest) {
    return this.http.post<ClientResponse>('api/auth/set-password', request);
  }

  uploadProfilePicture(data: FormData) {
    return this.http.post<ClientResponse<UploadResponse>>(
      'api/storage/profile-picture',
      data
    );
  }

  getLoginMethods() {
    return this.http
      .get<ClientResponse<LoginMethods>>('api/auth/login-methods')
      .pipe(map((r) => r.payload ?? { providers: [], hasPassword: false }));
  }
}
