import { HttpClient, HttpEvent } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { FileUploadResult } from '@core/models/view-models/workspace-file-view-model';
import { firstValueFrom, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class WorkspaceFilesService {
  private readonly http = inject(HttpClient);

  uploadTaskFile(systemId: string, file: File): Observable<HttpEvent<ClientResponse<FileUploadResult>>> {
    const form = new FormData();

    form.append('files', file, file.name);

    return this.http.request<ClientResponse<FileUploadResult>>(
      'POST',
      `api/tasks/${encodeURIComponent(systemId)}/files`,
      {
        body: form,
        observe: 'events',
        reportProgress: true,
      }
    );
  }

  deleteTaskFile(systemId: string, fileId: number): Promise<ClientResponse> {
    return firstValueFrom(
      this.http.delete<ClientResponse>(
        `api/tasks/${encodeURIComponent(systemId)}/files/${fileId}`
      )
    );
  }

  deleteFile(id: number) {
    return this.http.delete<ClientResponse>(`api/storage/files/${id}`);
  }
}
