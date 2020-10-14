export interface ClientResponse<TPayload = {}> {
  isSuccess: boolean;
  message?: string;
  payload?: TPayload;
}
