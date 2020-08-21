export interface ClientResponse {
  isSuccess: boolean;
  message?: string;
}

export interface ClientResponsePayload<TPayload> extends ClientResponse {
  payload?: TPayload;
}
