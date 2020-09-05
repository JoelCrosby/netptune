import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AddBoardRequest } from '@app/core/models/requests/add-board-request';
import { ClientResponsePayload } from '@core/models/client-response';
import { IsSlugUniqueResponse } from '@core/models/is-slug-unique-response';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { environment } from '@env/environment';

@Injectable()
export class BoardsService {
  constructor(private http: HttpClient) {}

  get(projectId: number) {
    return this.http.get<BoardViewModel[]>(
      environment.apiEndpoint + `api/boards`,
      {
        params: new HttpParams().append('projectId', projectId.toString()),
      }
    );
  }

  getByWorksapce(slug: string) {
    return this.http.get<BoardViewModel[]>(
      environment.apiEndpoint + `api/boards/workspace/${slug}`
    );
  }

  post(request: AddBoardRequest) {
    return this.http.post<ClientResponsePayload<BoardViewModel>>(
      environment.apiEndpoint + 'api/boards',
      request
    );
  }

  delete(boardId: number) {
    return this.http.delete<ClientResponsePayload<BoardViewModel>>(
      environment.apiEndpoint + `api/boards/${boardId}`
    );
  }

  isIdentifierUnique(identifier: string) {
    return this.http.get<ClientResponsePayload<IsSlugUniqueResponse>>(
      environment.apiEndpoint + `api/boards/is-unique/${identifier}`
    );
  }
}
