import { Maybe } from '../core/types/nothing';

export class ApiResult {

  constructor(public isSuccess: boolean, public message: string = 'An error has occured') {

  }

  static Success(): ApiResult {
    return new ApiResult(true);
  }

  static Error(message: string): ApiResult {
    return new ApiResult(false, message);
  }

  static FromError({ error }: any, fallback: string = 'An Unexpected error as occured.') {

    if (error == null) {
      return ApiResult.Error(fallback);
    }

    if (error.constructor !== Array && (typeof error === 'string' || error instanceof String)) {
      return ApiResult.Error(error as string);
    }

    if (error.length > 0) {

      let msg = '';
      for (let i = 0; i < error.length; i++) {
        const el = error[i];
        if (el.description) {
          msg += el.description + ' \n';
        }
      }
      return ApiResult.Error(msg);
    }

    return ApiResult.Error('Login failed');
  }
}
