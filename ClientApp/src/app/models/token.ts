export interface IToken {
    email: string;
    username: string;
    displayName: string;
    token: string;
    token_type: string;
    expires_in: number;
    issued: string;
    expires: string;
}

export class Token implements IToken {
    email = '';
    username = '';
    displayName = '';
    token = '';
    token_type = '';
    expires_in = 0;
    issued = '';
    expires = '';
}
