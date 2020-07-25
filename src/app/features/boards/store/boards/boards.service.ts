import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Board } from '@core/models/board';
import { environment } from '@env/environment';
import { BoardViewModel } from '@core/models/view-models/board-view-model';

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

  post(board: Board) {
    return this.http.post<BoardViewModel>(
      environment.apiEndpoint + 'api/boards',
      board
    );
  }

  delete(board: Board) {
    return this.http.delete<BoardViewModel>(
      environment.apiEndpoint + `api/boards/${board.id}`
    );
  }
}
