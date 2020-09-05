import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BoardGroup } from '@app/core/models/board-group';
import { BoardGroupsViewModel } from '@app/core/models/view-models/board-groups-view-model';
import { environment } from '@env/environment';
import { AddBoardGroupRequest } from '@app/core/models/add-board-group-request';
import { Params } from '@angular/router';
import { ClientResponse } from '@app/core/models/client-response';

@Injectable()
export class BoardGroupsService {
  constructor(private http: HttpClient) {}

  get(boardId: string, params: Params) {
    return this.http.get<BoardGroupsViewModel>(
      environment.apiEndpoint + `api/boardgroups/board/${boardId}`,
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
    return this.http.post<BoardGroup>(
      environment.apiEndpoint + 'api/tasks/movetaskingrouprequest',
      request
    );
  }

  delete(boardGorup: BoardGroup) {
    return this.http.delete<ClientResponse>(
      environment.apiEndpoint + `api/boardgroups/${boardGorup.id}`
    );
  }

  put(boardGorup: BoardGroup) {
    return this.http.put<BoardGroup>(
      environment.apiEndpoint + 'api/boardgroups',
      boardGorup
    );
  }
}
