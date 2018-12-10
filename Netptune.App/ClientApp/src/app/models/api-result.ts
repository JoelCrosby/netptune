export class ApiResult {

    constructor(public isSuccess: boolean, public message: string = null) {

    }

    static Success(): ApiResult {
        return new ApiResult(true);
    }

    static Error(message: string): ApiResult {
        return new ApiResult(false, message);
    }

    static FromError(error: any, fallback: string = 'An Unexpected error as occured.') {
        if (error.error.length > 0) {
            let msg = '';
            for (let i = 0; i < error.error.length; i++) {
                const el = error.error[i];
                if (el.description) {
                    msg += el.description + ' \n';
                }
            }
            return ApiResult.Error(msg);
        }

        return ApiResult.Error(fallback);
    }
}
