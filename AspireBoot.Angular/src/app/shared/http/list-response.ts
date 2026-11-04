export interface ListResponse<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly size: number;
  readonly count: number;
}
