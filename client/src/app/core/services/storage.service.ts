import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { UploadResponse } from '@core/models/upload-result';
import { environment } from '@env/environment';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class StorageService {
  private http = inject(HttpClient);


  uploadMedia(file: File): Observable<ClientResponse<UploadResponse>> {
    const formData = new FormData();
    formData.append('files', file);

    return this.http.post<ClientResponse<UploadResponse>>(
      environment.apiEndpoint + `api/storage/media/`,
      formData
    );
  }
}
