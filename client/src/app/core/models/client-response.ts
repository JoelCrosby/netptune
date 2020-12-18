// eslint-disable-next-line @typescript-eslint/ban-types
export interface ClientResponse<TPayload = {}> {
  isSuccess: boolean;
  message?: string;
  payload?: TPayload;
}
