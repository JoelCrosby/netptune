import { ClientResponse } from '@core/models/client-response';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

// eslint-disable-next-line @typescript-eslint/ban-types
export const unwrapClientReposne = <T>() => <P = {}>(
  source: Observable<T extends ClientResponse<P> ? T : null>
): Observable<P> =>
  source.pipe(
    map((response) => {
      if (response.isSuccess) {
        return response.payload;
      } else {
        throw new Error(
          `Server responded with failure${
            response.message ? ': ' + response.message : ''
          }`
        );
      }
    })
  );
