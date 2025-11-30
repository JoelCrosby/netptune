export interface ClientResponse<TPayload = object> {
  isSuccess: boolean;
  message?: string;
  payload?: TPayload;
}
