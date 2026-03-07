export interface BaseListResponse<T> {
  data?: T[];
  currentPage: number;
  totalPages: number;
  totalItems: number;
}
