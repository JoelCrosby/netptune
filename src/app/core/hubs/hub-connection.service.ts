import { Injectable } from '@angular/core';
import { selectAuthToken } from '@core/auth/store/auth.selectors';
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

    const baseUrl = `${environment.apiEndpoint}${path}`;
    const connection = new HubConnectionBuilder()
      .withUrl(baseUrl, {
        accessTokenFactory: () => token,
      } as IHttpConnectionOptions)
      .build();

    if (!handlers.length) {
      console.warn(
        '[SIGNAL-R ]connect was called before registering any handlers'
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
    if (connection.state === HubConnectionState.Disconnected) {
      try {
        await connection.start();

        console.log(
          `%c[SIGNAL-R][Connected] id: ${connection.connectionId}`,
          'color: lime'
        );
      } catch (err) {
        return console.error(`[SIGNAL-R][ERROR] ${err}`);
      }
    }

    return null;
  }

  async stop(connection: HubConnection) {
    if (!connection) {
      console.warn('[SIGNAL-R] attmpted to close a null connection.');

      return;
    }

    const connectionId = connection.connectionId;

    delete this.connections[connection.baseUrl];

    await connection.stop();

    console.log(
      `%c[SIGNAL-R][Disconnected] id: ${connectionId}`,
      'color: orange'
    );
  }
}
