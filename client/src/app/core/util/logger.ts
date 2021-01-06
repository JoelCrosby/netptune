import { environment } from '@env/environment';

export class Logger {
  static log(message?: unknown, ...optionalParams: unknown[]) {
    if (!environment.production) {
      console.log(message, ...optionalParams);
    }
  }

  static warn(message?: unknown, ...optionalParams: unknown[]) {
    if (!environment.production) {
      console.warn(message, ...optionalParams);
    }
  }

  static error(message?: unknown, ...optionalParams: unknown[]) {
    if (!environment.production) {
      console.error(message, ...optionalParams);
    }
  }
}
