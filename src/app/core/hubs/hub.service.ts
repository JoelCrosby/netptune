import { Injectable } from '@angular/core';
import { HubConnection } from '@microsoft/signalr';
import { Action, Store } from '@ngrx/store';
import { from, Observable, throwError } from 'rxjs';
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
  private handlers: HubMethodHandler[] = [];

  constructor(
    private connetionService: HubConnectionService,
    private store: Store
  ) {}

  connect(handlers: HubMethodHandler[]): Promise<void> {
    this.handlers = [...this.handlers, ...handlers];
    return this.openConnection();
  }

  invoke<TResult>(method: string, ...args: unknown[]): Observable<TResult> {
    return from(this.connection.invoke<TResult>(method, ...args)).pipe(
      catchError((err: HubError) => {
        console.error(`[SIGNAL-R][HUB-ERROR] ${err.message}`, err.stack);

        return throwError(err.message);
      })
    );
  }

  dispatch(action: Action) {
    return this.store.dispatch(redirectAction(action));
  }

  private async openConnection(): Promise<void> {
    this.connection = await this.connetionService.connect('hubs/board-hub');

    if (!this.handlers.length) {
      console.warn(
        '[SIGNAL-R ]connect was called before registering any handlers'
      );
    }

    this.handlers.forEach(({ method, callback }) =>
      this.connection.on(method, callback)
    );

    this.connetionService.start('hubs/board-hub');
  }
}
