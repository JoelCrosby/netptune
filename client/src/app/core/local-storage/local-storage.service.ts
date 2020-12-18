import { Injectable } from '@angular/core';

const APP_PREFIX = 'Netptune-';

@Injectable({
  providedIn: 'root',
})
export class LocalStorageService {
  constructor() {}

  // eslint-disable-next-line @typescript-eslint/ban-types
  static loadInitialState<TState extends object>() {
    return Object.keys(localStorage).reduce((state: TState, storageKey) => {
      if (storageKey.includes(APP_PREFIX)) {
        const stateKeys: (keyof TState)[] = storageKey
          .replace(APP_PREFIX, '')
          .toLowerCase()
          .split('.')
          .map(
            (key) =>
              key
                .split('-')
                .map((token, index) =>
                  index === 0
                    ? token
                    : token.charAt(0).toUpperCase() + token.slice(1)
                )
                .join('') as keyof TState
          );
        let currentStateRef = state;
        stateKeys.forEach((key, index) => {
          if (index === stateKeys.length - 1) {
            const item = localStorage.getItem(storageKey);
            if (!item) {
              return;
            }
            currentStateRef[key] = JSON.parse(item);
            return;
          }

          currentStateRef[key] =
            currentStateRef[key] || ({} as TState[keyof TState]);
          currentStateRef = (currentStateRef[key] as unknown) as TState;
        });
      }
      return state;
    }, {});
  }

  setItem(key: string, value: unknown) {
    localStorage.setItem(`${APP_PREFIX}${key}`, JSON.stringify(value));
  }

  getItem<T>(key: string): T | undefined {
    const item = localStorage.getItem(`${APP_PREFIX}${key}`);
    if (!item) {
      return undefined;
    }
    return JSON.parse(item);
  }

  removeItem(key: string) {
    localStorage.removeItem(`${APP_PREFIX}${key}`);
  }

  /** Tests that localStorage exists, can be written to, and read from. */
  testLocalStorage() {
    const testValue = 'testValue';
    const testKey = 'testKey';
    const errorMessage = 'localStorage did not return expected value';

    this.setItem(testKey, testValue);
    const retrievedValue = this.getItem(testKey);
    this.removeItem(testKey);

    if (retrievedValue !== testValue) {
      throw new Error(errorMessage);
    }
  }
}
