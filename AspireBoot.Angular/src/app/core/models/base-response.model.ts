export interface BaseResponse<T = unknown> {
  data?: T;
  errorMessage?: string;
}
