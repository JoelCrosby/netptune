import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  CreatePageRequest,
  ImportPagesResult,
  MovePageRequest,
  PageDetail,
  SavePageRequest,
  TransformPageTextRequest,
  TransformPageTextResult,
  UpdatePageRequest,
} from '@core/models/pages';

@Service()
export class PagesService {
  private readonly http = inject(HttpClient);

  create(request: CreatePageRequest) {
    return this.http.post<ClientResponse<PageDetail>>('api/pages', request);
  }

  save(id: number, request: SavePageRequest) {
    return this.http.put<ClientResponse<PageDetail>>(
      `api/pages/${id}`,
      request
    );
  }

  update(id: number, request: UpdatePageRequest) {
    return this.http.patch<ClientResponse<PageDetail>>(
      `api/pages/${id}`,
      request
    );
  }

  restoreVersion(id: number, versionId: number) {
    return this.http.post<ClientResponse<PageDetail>>(
      `api/pages/${id}/versions/${versionId}/restore`,
      null
    );
  }

  move(id: number, request: MovePageRequest) {
    return this.http.patch<ClientResponse<PageDetail>>(
      `api/pages/${id}/move`,
      request
    );
  }

  archive(id: number) {
    return this.http.delete<ClientResponse>(`api/pages/${id}`);
  }

  restore(id: number) {
    return this.http.post<ClientResponse<PageDetail>>(
      `api/pages/${id}/restore`,
      null
    );
  }

  transform(request: TransformPageTextRequest) {
    return this.http.post<ClientResponse<TransformPageTextResult>>(
      'api/ai/pages/transform',
      request
    );
  }

  import(files: readonly File[], projectId: number | null) {
    const form = new FormData();

    for (const file of files) {
      form.append('files', file, file.name);
    }

    const params: Record<string, number> =
      projectId === null ? {} : { projectId };

    return this.http.post<ClientResponse<ImportPagesResult>>(
      'api/pages/import',
      form,
      { params }
    );
  }

  markReviewed(id: number) {
    return this.http.post<ClientResponse>(`api/pages/${id}/reviewed`, null);
  }

  favourite(id: number) {
    return this.http.put<ClientResponse>(`api/pages/${id}/favourite`, null);
  }

  unfavourite(id: number) {
    return this.http.delete<ClientResponse>(`api/pages/${id}/favourite`);
  }

  recordVisit(id: number) {
    return this.http.post<ClientResponse>(`api/pages/${id}/visits`, null);
  }
}
