import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, firstValueFrom, map, of } from 'rxjs';

/** One field's failure inside a 422, keyed by the field name in the request body. */
export interface FieldProblem {
  code: string;
  message: string;
}

/**
 * A refused call as RFC 9457 problem details (L2-068). status 0 with code NETWORK means the
 * request never reached TuTuMuch API; retryAfter is in seconds for 429.
 */
export interface ApiProblem {
  status: number;
  code: string;
  title: string;
  correlationId: string | null;
  errors: Record<string, FieldProblem>;
  retryAfter: number | null;
}

export type ApiResult<T> = { ok: true; value: T } | { ok: false; problem: ApiProblem };

/** Formats a correlation ID for display, for example "Ref 5c1d-0e7a". */
export function shortRef(correlationId: string | null): string {
  const id = (correlationId ?? '').replace(/[^0-9a-f]/gi, '');
  return id ? `Ref ${id.slice(0, 4)}-${id.slice(4, 8)}` : '';
}

function toProblem(error: unknown): ApiProblem {
  if (!(error instanceof HttpErrorResponse)) {
    return {
      status: 0,
      code: 'NETWORK',
      title: 'Check your connection.',
      correlationId: null,
      errors: {},
      retryAfter: null,
    };
  }
  const body = (typeof error.error === 'object' && error.error) || {};
  const retryAfter = Number(error.headers?.get('Retry-After'));
  return {
    status: error.status,
    code: body.code ?? (error.status === 0 ? 'NETWORK' : 'UNKNOWN'),
    title: body.title ?? error.statusText,
    correlationId: body.correlationId ?? null,
    errors: body.errors ?? {},
    retryAfter: Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : null,
  };
}

/** Runs a request and resolves to a typed result instead of throwing. */
export function toResult<T>(request: Observable<T>): Promise<ApiResult<T>> {
  return firstValueFrom(
    request.pipe(
      map((value): ApiResult<T> => ({ ok: true, value })),
      catchError((error: unknown) => of<ApiResult<T>>({ ok: false, problem: toProblem(error) })),
    ),
  );
}
