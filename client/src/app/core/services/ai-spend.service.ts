import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { AiSpend, SetAiSpendCapRequest } from '@core/models/ai-spend';
import { ClientResponse } from '@core/models/client-response';
import { aiSpendCapUrl } from '@core/resources/ai-spend.resource';

@Service()
export class AiSpendService {
  private readonly http = inject(HttpClient);

  setCap(request: SetAiSpendCapRequest) {
    return this.http.put<ClientResponse<AiSpend>>(aiSpendCapUrl, request);
  }
}
