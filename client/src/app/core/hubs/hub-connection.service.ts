import { Injectable } from '@angular/core';
import { selectAuthToken } from '@core/auth/store/auth.selectors';
import { Logger } from '@core/util/logger';
import { environment } from '@env/environment';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  IHttpConnectionOptions,
} from '@microsoft/signalr';
import { Store } from '@ngrx/store';
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

  constructor(private store: Store) {}

  async connect(
    path: string,
    handlers: HubMethodHandler[]
  ): Promise<HubConnection> {
    if (this.connections[path]) {
      return this.connections[path];
    }

    const token = await this.store
      .select(selectAuthToken)
      .pipe(first())
      .toPromise();

    const httpOptions: IHttpConnectionOptions = {
      accessTokenFactory: () => token,
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
      return null;
    }

    try {
      await connection.start();

      Logger.log(
        `%c[SIGNAL-R][Connected] id: ${connection.connectionId}`,
        'color: lime'
      );
    } catch (err) {
      return console.error(`[SIGNAL-R][ERROR] ${err}`);
    }
  }

  async stop(connection: HubConnection) {
    if (!connection) {
      return Logger.warn('[SIGNAL-R] attmpted to close a null connection.');
    }

    const connectionId = connection.connectionId;
    delete this.connections[connection.baseUrl];

    await connection.stop();

    Logger.log(
      `%c[SIGNAL-R][Disconnected] id: ${connectionId}`,
      'color: orange'
    );
  }
}
