import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Params } from '@angular/router';
import { AddBoardGroupRequest } from '@core/models/add-board-group-request';
import { ClientResponse } from '@core/models/client-response';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { BoardGroupViewModel } from '@core/models/view-models/board-group-view-model';
import { BoardView, BoardViewGroup } from '@core/models/view-models/board-view';
import { FileResponse } from '@core/types/file-response';
import { extractFilenameFromHeaders } from '@core/util/header-utils';
import { environment } from '@env/environment';
import { Observable, of, throwError } from 'rxjs';
import { switchMap } from 'rxjs/operators';

@Injectable()
export class BoardGroupsService {
  constructor(private http: HttpClient) {}

  get(boardId: string, params: Params) {
    return this.http.get<ClientResponse<BoardView>>(
      environment.apiEndpoint + `api/boards/view/${boardId}`,
      {
        params,
      }
    );
  }

  post(request: AddBoardGroupRequest) {
    return this.http.post<ClientResponse<BoardGroupViewModel>>(
      environment.apiEndpoint + 'api/boardgroups',
      request
    );
  }

  moveTaskInBoardGroup(request: MoveTaskInGroupRequest) {
    return this.http.post<ClientResponse>(
      environment.apiEndpoint + 'api/tasks/move-task-in-group',
      request
    );
  }

  delete(boardGorupId: number) {
    return this.http.delete<ClientResponse>(
      environment.apiEndpoint + `api/boardgroups/${boardGorupId}`
    );
  }

  put(boardGorup: BoardGroupViewModel | BoardViewGroup) {
    return this.http.put<ClientResponse<BoardGroupViewModel>>(
      environment.apiEndpoint + 'api/boardgroups',
      boardGorup
    );
  }

  export(boardId: string): Observable<FileResponse> {
    return this.http
      .get(
        environment.apiEndpoint + `api/export/tasks/export-board/${boardId}`,
        {
          observe: 'response',
          responseType: 'blob',
        }
      )
      .pipe(
        switchMap((response) => {
          if (response.body === null) {
            return throwError('repsone body was null');
          }

          return of({
            file: response.body,
            filename: extractFilenameFromHeaders(response.headers),
          });
        })
      );
  }
}
