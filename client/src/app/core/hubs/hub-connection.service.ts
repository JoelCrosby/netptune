import { Injectable } from '@angular/core';
import { selectAuthToken } from '@core/auth/store/auth.selectors';
import { AppState } from '@core/core.state';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  IHttpConnectionOptions,
  LogLevel,
} from '@microsoft/signalr';
import { Store } from '@ngrx/store';
import { firstValueFrom } from 'rxjs';
import { first } from 'rxjs/operators';
import { HubMethodHandler } from './hub.service';

interface ConnectionMap {
  [baseUrl: string]: HubConnection;
}

@Injectable({
  providedIn: 'root',
})
export class HubConnectionService {
  connections: ConnectionMap = {};

  constructor(private store: Store<AppState>) {}

  async connect(
    path: string,
    handlers: HubMethodHandler[]
  ): Promise<HubConnection> {
    if (this.connections[path]) {
      return this.connections[path];
    }

    const tokenObservable = this.store.select(selectAuthToken).pipe(first());
    const token = await firstValueFrom(tokenObservable, {
      defaultValue: undefined,
    });

    if (token === undefined) {
      throw new Error(
        'Unable to connect to hub, authentication token not present.'
      );
    }

    const httpOptions: IHttpConnectionOptions = {
      accessTokenFactory: () => token,
      logger: environment.production ? LogLevel.Critical : undefined,
    };

    const baseUrl = `${environment.apiEndpoint}${path}`;
    const connection = new HubConnectionBuilder()
      .withUrl(baseUrl, httpOptions)
      .build();

    if (!handlers.length) {
      console.warn(
        '[SIGNAL-R] connect was called before registering any handlers'
      );
    }

    handlers.forEach(({ method, callback }) => connection.on(method, callback));

    this.connections = {
      ...this.connections,
      [baseUrl]: connection,
    };

    await this.start(connection);

    return connection;
  }

  async start(connection: HubConnection): Promise<void> {
    if (connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    try {
      await connection.start();

      connection.connectionId &&
        Logger.log(
          `%c[SIGNAL-R][Connected] id: ${connection.connectionId}`,
          'color: lime'
        );
    } catch (err) {
      if (typeof err === 'string') {
        return console.error(`[SIGNAL-R][ERROR] ${err}`);
      }
    }
  }

  async stop(connection: HubConnection) {
    if (!connection) {
      return Logger.warn('[SIGNAL-R] attmpted to close a null connection.');
    }

    const connectionId = connection.connectionId;
    delete this.connections[connection.baseUrl];

    await connection.stop();

    connectionId &&
      Logger.log(
        `%c[SIGNAL-R][Disconnected] id: ${connectionId}`,
        'color: orange'
      );
  }
}
