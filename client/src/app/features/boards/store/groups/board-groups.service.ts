import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Params } from '@angular/router';
import { AddBoardGroupRequest } from '@core/models/add-board-group-request';
import { BoardGroup } from '@core/models/board-group';
import { ClientResponse } from '@core/models/client-response';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { BoardView, BoardViewGroup } from '@core/models/view-models/board-view';
import { environment } from '@env/environment';

@Injectable()
export class BoardGroupsService {
  constructor(private http: HttpClient) {}

  get(boardId: string, params: Params) {
    return this.http.get<BoardView>(
      environment.apiEndpoint + `api/boards/view/${boardId}`,
      {
        params,
      }
    );
  }

  post(request: AddBoardGroupRequest) {
    return this.http.post<BoardGroup>(
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

  put(boardGorup: BoardGroup | BoardViewGroup) {
    return this.http.put<BoardGroup>(
      environment.apiEndpoint + 'api/boardgroups',
      boardGorup
    );
  }
}
