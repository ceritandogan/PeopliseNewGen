import { isAxiosError } from "axios";

/** A normalized shape for whatever a failed request's body is — matches ASP.NET Core's `ProblemDetails`, assuming controllers surface `Result` failures that way (they don't exist yet — see the module-level caveat in `types.ts`). */
export interface ApiErrorDetails {
  status: number;
  title: string;
  detail?: string;
}

export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail?: string;

  constructor({ status, title, detail }: ApiErrorDetails) {
    super(title);
    this.name = "ApiError";
    this.status = status;
    this.title = title;
    this.detail = detail;
  }
}

/** Normalizes any error a resource-module call can throw into an `ApiError`, for consistent handling in TanStack Query's `onError`/`error` state. */
export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;

  if (isAxiosError(error)) {
    const status = error.response?.status ?? 0;
    const problem = error.response?.data as { title?: string; detail?: string } | undefined;
    return new ApiError({
      status,
      title: problem?.title ?? error.message,
      detail: problem?.detail,
    });
  }

  return new ApiError({ status: 0, title: error instanceof Error ? error.message : "Unknown error" });
}
