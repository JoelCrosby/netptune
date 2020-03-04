import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BoardGroup } from '@app/core/models/board-group';
import { environment } from '@env/environment';

@Injectable()
export class BoardGroupsService {
  constructor(private http: HttpClient) {}

  get(boardId: number) {
    return this.http.get<BoardGroup[]>(
      environment.apiEndpoint + `api/boardgroups`,
      {
        params: new HttpParams().append('boardId', boardId.toString()),
      }
    );
  }

  post(boardGorup: BoardGroup) {
    return this.http.post<BoardGroup>(
      environment.apiEndpoint + 'api/boardgroups',
      boardGorup
    );
  }

  moveTaskInBoardGroup(request: MoveTaskInGroupRequest) {
    return this.http.post<BoardGroup>(
      environment.apiEndpoint + 'api/porojecttasks/movetaskingrouprequest',
      request
    );
  }

  delete(boardGorup: BoardGroup) {
    return this.http.delete<BoardGroup>(
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
