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

interface ConnectionMap {
  [path: string]: HubConnection;
}

@Injectable({
  providedIn: 'root',
})
export class HubConnectionService {
  connections: ConnectionMap = {};

  constructor(private store: Store) {}

  async connect(path: string): Promise<HubConnection> {
    if (this.connections[path]) {
      return new Promise((res) => res(this.connections[path]));
    }

    const token = await this.store
      .select(selectAuthToken)
      .pipe(first())
      .toPromise();

    const connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiEndpoint}${path}`, {
        accessTokenFactory: () => token,
      } as IHttpConnectionOptions)
      .build();

    this.connections = {
      ...this.connections,
      [path]: connection,
    };

    return connection;
  }

  start(path: string): void {
    const connection = this.connections[path];

    if (!connection) {
      throw new Error(
        `[SIGNAL-R] connection with path: ${path} does not exist.`
      );
    }

    if (connection.state === HubConnectionState.Disconnected) {
      connection
        .start()
        .then(() => {
          console.log(
            `%c[SIGNAL-R][Connected] id: ${connection.connectionId}`,
            'color: lime'
          );
        })
        .catch((err) => console.error(`[SIGNAL-R][ERROR] ${err}`));
    }
  }
}
