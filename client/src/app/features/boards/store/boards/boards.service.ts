import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { IsSlugUniqueResponse } from '@core/models/is-slug-unique-response';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { UpdateBoardRequest } from '@core/models/requests/update-board-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { BoardsViewModel } from '@core/models/view-models/boards-view-model';
import { environment } from '@env/environment';

@Injectable({ providedIn: 'root' })
export class BoardsService {
  private http = inject(HttpClient);

  get(projectId: number) {
    return this.http.get<BoardViewModel[]>(
      environment.apiEndpoint + `api/boards`,
      {
        params: new HttpParams().append('projectId', projectId.toString()),
      }
    );
  }

  getByWorkspace() {
    return this.http.get<BoardsViewModel[]>(
      environment.apiEndpoint + `api/boards/workspace`
    );
  }

  post(request: AddBoardRequest) {
    return this.http.post<ClientResponse<BoardViewModel>>(
      environment.apiEndpoint + 'api/boards',
      request
    );
  }

  put(request: UpdateBoardRequest) {
    return this.http.put<ClientResponse<BoardViewModel>>(
      environment.apiEndpoint + 'api/boards',
      request
    );
  }

  delete(boardId: number) {
    return this.http.delete<ClientResponse>(
      environment.apiEndpoint + `api/boards/${boardId}`
    );
  }

  isIdentifierUnique(identifier: string) {
    return this.http.get<ClientResponse<IsSlugUniqueResponse>>(
      environment.apiEndpoint + `api/boards/is-unique/${identifier}`
    );
  }
}
