import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  ApiCredentialCreated,
  CreateApiCredentialRequest,
  CreateServiceAccountRequest,
  ServiceAccount,
} from '@core/models/service-account';

@Injectable({ providedIn: 'root' })
export class ServiceAccountsService {
  private readonly http = inject(HttpClient);

  getAll() {
    return this.http.get<ServiceAccount[]>('api/service-accounts');
  }

  create(request: CreateServiceAccountRequest) {
    return this.http.post<ServiceAccount>('api/service-accounts', request);
  }

  createCredential(
    serviceAccountId: number,
    request: CreateApiCredentialRequest
  ) {
    return this.http.post<ApiCredentialCreated>(
      `api/service-accounts/${serviceAccountId}/credentials`,
      request
    );
  }

  revokeCredential(serviceAccountId: number, credentialId: string) {
    return this.http.delete(
      `api/service-accounts/${serviceAccountId}/credentials/${credentialId}`
    );
  }
}
