import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  AiCredential,
  AiCredentialScope,
  SaveAiCredentialRequest,
} from '@core/models/ai-credential';
import { ClientResponse } from '@core/models/client-response';
import { aiCredentialUrl } from '@core/resources/ai-credential.resource';

@Injectable({ providedIn: 'root' })
export class AiCredentialsService {
  private readonly http = inject(HttpClient);

  save(request: SaveAiCredentialRequest, scope: AiCredentialScope = 'user') {
    return this.http.put<ClientResponse<AiCredential>>(
      aiCredentialUrl(scope),
      request
    );
  }

  delete(credentialId: string, scope: AiCredentialScope = 'user') {
    return this.http.delete(`${aiCredentialUrl(scope)}/${credentialId}`);
  }
}
