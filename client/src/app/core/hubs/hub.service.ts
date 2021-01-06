import { Injectable } from '@angular/core';
import { Logger } from '@core/util/logger';
import { HubConnection } from '@microsoft/signalr';
import { Action, Store } from '@ngrx/store';
import { from, Observable, throwError } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { selectCurrentWorkspaceIdentifier } from '../store/workspaces/workspaces.selectors';
import { HubConnectionService } from './hub-connection.service';
import { redirectAction } from './hub.utils';

export interface HubError {
  message: string;
  stack: string;
}

export interface HubMethodHandler {
  method: string;
  callback: (...args: unknown[]) => void;
}

@Injectable({
  providedIn: 'root',
})
export class HubService {
  private connection: HubConnection;

  constructor(
    private connetionService: HubConnectionService,
    private store: Store
  ) {}

  connect(path: string, handlers: HubMethodHandler[]): Promise<void> {
    return this.openConnection(path, handlers);
  }

  disconnect(): Promise<void> {
    return this.connetionService.stop(this.connection);
  }

  invoke<TResult>(
    method: string,
    group: string,
    payload: unknown = {}
  ): Observable<TResult> {
    if (!group) {
      return throwError('group argument must be provided');
    }

    return this.store.select(selectCurrentWorkspaceIdentifier).pipe(
      switchMap((workspaceKey) =>
        from(
          this.connection.invoke<TResult>(method, {
            group,
            workspaceKey,
            payload,
          })
        ).pipe(
          tap((res) => Logger.log(`[SIGNAL-R][RESPONSE] `, { res })),
          catchError((err: HubError) => {
            Logger.error(`[SIGNAL-R][HUB-ERROR] ${err.message}`, err.stack);

            return throwError(err.message);
          })
        )
      )
    );
  }

  dispatch(action: Action, redirect: boolean = false) {
    return redirect
      ? this.store.dispatch(redirectAction(action))
      : this.store.dispatch(action);
  }

  private async openConnection(
    path: string,
    handlers: HubMethodHandler[]
  ): Promise<void> {
    this.connection = await this.connetionService.connect(
      `hubs/${path}`,
      handlers
    );
  }
}
