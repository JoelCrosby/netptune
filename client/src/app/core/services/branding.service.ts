import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { BrandingImage, BrandingTarget } from '@core/models/branding';
import { ClientResponse } from '@core/models/client-response';
import { Observable } from 'rxjs';

@Service()
export class BrandingService {
  private readonly http = inject(HttpClient);

  upload(
    target: BrandingTarget,
    file: File
  ): Observable<ClientResponse<BrandingImage>> {
    const formData = new FormData();
    formData.append('image', file, file.name);

    return this.http.post<ClientResponse<BrandingImage>>(
      brandingUrl(target),
      formData
    );
  }

  remove(target: BrandingTarget): Observable<ClientResponse> {
    return this.http.delete<ClientResponse>(brandingUrl(target));
  }
}

function brandingUrl(target: BrandingTarget): string {
  switch (target.kind) {
    case 'workspaceLogo':
      return 'api/workspaces/branding/logo';
    case 'projectLogo':
      return `api/projects/${target.projectId}/branding/logo`;
    case 'boardLogo':
      return `api/boards/${target.boardId}/branding/logo`;
    case 'boardBackground':
      return `api/boards/${target.boardId}/branding/background`;
  }
}
