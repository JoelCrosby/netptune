import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  SaveSearchCredentialRequest,
  SearchCredential,
} from '@core/models/search-credential';
import { searchCredentialUrl } from '@core/resources/search-credential.resource';

@Injectable({ providedIn: 'root' })
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
