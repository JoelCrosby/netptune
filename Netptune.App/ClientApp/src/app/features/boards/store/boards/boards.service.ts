import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Board } from '@app/core/models/board';
import { environment } from '@env/environment';

@Injectable()
export class BoardsService {
  constructor(private http: HttpClient) {}

  get(projectId: number) {
    return this.http.get<Board[]>(environment.apiEndpoint + `api/boards`, {
      params: new HttpParams().append('projectId', projectId.toString()),
    });
  }

  post(board: Board) {
    return this.http.post<Board>(environment.apiEndpoint + 'api/boards', board);
  }

  delete(board: Board) {
    return this.http.delete<Board>(
      environment.apiEndpoint + `api/boards/${board.id}`
    );
  }
}
