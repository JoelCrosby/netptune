import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { IsSlugUniqueResponse } from '@core/models/is-slug-unique-response';
import { appendPageParams, MAX_PAGE_SIZE } from '@core/models/pagination';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { UpdateBoardRequest } from '@core/models/requests/update-board-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { BoardsViewModel } from '@core/models/view-models/boards-view-model';

@Injectable({ providedIn: 'root' })
export class BoardsService {
  private http = inject(HttpClient);

  get(projectId: number) {
    return this.http.get<BoardViewModel[]>(`api/boards/project/${projectId}`, {
      params: appendPageParams(new HttpParams(), { pageSize: MAX_PAGE_SIZE }),
    });
  }

  getByWorkspace() {
    return this.http.get<BoardsViewModel[]>(`api/boards/workspace`, {
      params: appendPageParams(new HttpParams(), { pageSize: MAX_PAGE_SIZE }),
    });
  }

  post(request: AddBoardRequest) {
    return this.http.post<ClientResponse<BoardViewModel>>(
      'api/boards',
      request
    );
  }

  put(request: UpdateBoardRequest) {
    return this.http.put<ClientResponse<BoardViewModel>>('api/boards', request);
  }

  delete(boardId: number) {
    return this.http.delete<ClientResponse>(`api/boards/${boardId}`);
  }

  isIdentifierUnique(identifier: string) {
    return this.http.get<ClientResponse<IsSlugUniqueResponse>>(
      `api/boards/is-unique/${identifier}`
    );
  }
}
