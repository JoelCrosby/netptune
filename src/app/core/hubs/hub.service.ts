import { Injectable } from '@angular/core';
import { HubConnection } from '@microsoft/signalr';
import { Action, Store } from '@ngrx/store';
import { from, Observable, of, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
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
    ...args: unknown[]
  ): Observable<TResult> {
    if (!group) return of(null);

    return from(this.connection.invoke<TResult>(method, group, ...args)).pipe(
      catchError((err: HubError) => {
        console.error(`[SIGNAL-R][HUB-ERROR] ${err.message}`, err.stack);

        return throwError(err.message);
      })
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
