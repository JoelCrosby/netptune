import { AppUser } from '@core/models/appuser';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';

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

  put(user: Partial<AppUser>) {
    return this.http.put<AppUser>(
      environment.apiEndpoint + `api/users/${user.id}`,
      user
    );
  }
}
