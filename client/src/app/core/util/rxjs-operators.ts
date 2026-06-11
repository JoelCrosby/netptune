import { ClientResponse } from '@core/models/client-response';
import { Page } from '@core/models/pagination';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export const unwrapClientReposne =
  <T>() =>
  <P = unknown>(
    source: Observable<T extends ClientResponse<P> ? T : null>
  ): Observable<P> =>
    source.pipe(
      map((response) => {
        if (response?.isSuccess) {
          return response.payload as P;
        } else {
          throw new Error(
            `Server responded with failure${
              response?.message ? ': ' + response.message : ''
            }`
          );
        }
      })
    );

export const unwrapClientPageReposne =
  <T>() =>
  <P = unknown>(
    source: Observable<T extends ClientResponse<Page<P>> ? T : null>
  ): Observable<P[]> =>
    source.pipe(
      map((response) => {
        if (response?.isSuccess) {
          return (response.payload as Page<P>).items;
        } else {
          throw new Error(
            `Server responded with failure${
              response?.message ? ': ' + response.message : ''
            }`
          );
        }
      })
    );
