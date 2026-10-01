import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { UploadResponse } from '@core/models/upload-result';
import { Observable } from 'rxjs';

@Service()
export class StorageService {
  private http = inject(HttpClient);

  uploadMedia(
    file: File,
    taskSystemId?: string | null
  ): Observable<ClientResponse<UploadResponse>> {
    const formData = new FormData();
    formData.append('files', file);

    if (taskSystemId) {
      formData.append('taskSystemId', taskSystemId);
    }

    return this.http.post<ClientResponse<UploadResponse>>(
      'api/storage/media/',
      formData
    );
  }
}
