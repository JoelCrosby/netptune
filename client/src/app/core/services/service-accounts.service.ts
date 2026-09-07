import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import {
  ApiCredentialCreated,
  CreateApiCredentialRequest,
  CreateServiceAccountRequest,
  ServiceAccount,
  UpdateServiceAccountRequest,
} from '@core/models/service-account';
import { ClientResponse } from '@core/models/client-response';
import { unwrapClientResponse } from '@core/util/rxjs-operators';

@Service()
export class ServiceAccountsService {
  private readonly http = inject(HttpClient);

  getAll() {
    return this.http.get<ServiceAccount[]>('api/service-accounts');
  }

  create(request: CreateServiceAccountRequest) {
    return this.http
      .post<ClientResponse<ServiceAccount>>('api/service-accounts', request)
      .pipe(unwrapClientResponse());
  }

  update(serviceAccountId: number, request: UpdateServiceAccountRequest) {
    return this.http
      .put<ClientResponse<ServiceAccount>>(
        `api/service-accounts/${serviceAccountId}`,
        request
      )
      .pipe(unwrapClientResponse());
  }

  delete(serviceAccountId: number) {
    return this.http.delete(`api/service-accounts/${serviceAccountId}`);
  }

  createCredential(
    serviceAccountId: number,
    request: CreateApiCredentialRequest
  ) {
    return this.http
      .post<ClientResponse<ApiCredentialCreated>>(
        `api/service-accounts/${serviceAccountId}/credentials`,
        request
      )
      .pipe(unwrapClientResponse());
  }

  revokeCredential(serviceAccountId: number, credentialId: string) {
    return this.http.delete(
      `api/service-accounts/${serviceAccountId}/credentials/${credentialId}`
    );
  }
}
