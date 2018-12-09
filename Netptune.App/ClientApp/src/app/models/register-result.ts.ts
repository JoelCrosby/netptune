export class RegisterResult {

    constructor(public isSuccess: boolean, public message: string = null) {

    }

    static Success(): RegisterResult {
        return new RegisterResult(true);
    }

    static Error(message: string): RegisterResult {
        return new RegisterResult(false, message);
    }
}
