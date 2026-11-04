import { HttpErrorResponse } from '@angular/common/http';

export interface ApiProblem {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly code?: string;
  readonly traceId?: string;
  readonly codes?: Record<string, string[]>;
  readonly errors?: Record<string, string[]>;
}

export function apiProblem(error: unknown): ApiProblem {
  return error instanceof HttpErrorResponse && error.error && typeof error.error === 'object'
    ? (error.error as ApiProblem)
    : {};
}

export function problemCode(error: unknown, code: string): boolean {
  return apiProblem(error).code === code;
}

export function fieldCodes(error: unknown, field: string): string[] {
  const codes = apiProblem(error).codes?.[field];
  return Array.isArray(codes)
    ? codes.filter((value): value is string => typeof value === 'string')
    : [];
}

export function fieldErrors(error: unknown, field: string): string[] {
  const errors = apiProblem(error).errors?.[field];
  return Array.isArray(errors)
    ? errors.filter((value): value is string => typeof value === 'string')
    : [];
}
