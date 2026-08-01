import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  AiCredential,
  SaveAiCredentialRequest,
} from '@core/models/ai-credential';
import { ClientResponse } from '@core/models/client-response';

@Injectable({ providedIn: 'root' })
export class AiCredentialsService {
  private readonly http = inject(HttpClient);

  save(request: SaveAiCredentialRequest) {
    return this.http.put<ClientResponse<AiCredential>>(
      'api/ai/credentials',
      request
    );
  }

  delete(credentialId: string) {
    return this.http.delete(`api/ai/credentials/${credentialId}`);
  }
}
