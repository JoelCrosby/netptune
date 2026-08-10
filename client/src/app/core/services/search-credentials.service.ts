import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  SaveSearchCredentialRequest,
  SearchCredential,
} from '@core/models/search-credential';
import { searchCredentialUrl } from '@core/resources/search-credential.resource';

@Service()
export class SearchCredentialsService {
  private readonly http = inject(HttpClient);

  save(request: SaveSearchCredentialRequest) {
    return this.http.put<ClientResponse<SearchCredential>>(
      searchCredentialUrl,
      request
    );
  }

  delete() {
    return this.http.delete(searchCredentialUrl);
  }
}
