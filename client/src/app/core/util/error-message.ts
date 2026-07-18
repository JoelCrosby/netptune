import { HttpErrorResponse } from '@angular/common/http';

const SERVER_FAILURE_PREFIX = /^Server responded with failure(?::\s*)?/;

export function getErrorMessage(
  error: unknown,
  fallback = 'Something went wrong. Please try again.'
): string {
  if (error instanceof HttpErrorResponse) {
    return getResponseMessage(error.error) ?? fallback;
  }

  if (error instanceof Error) {
    return normalizeMessage(error.message) ?? fallback;
  }

  if (typeof error === 'string') {
    return normalizeMessage(error) ?? fallback;
  }

  return fallback;
}

function getResponseMessage(response: unknown): string | undefined {
  if (typeof response === 'string') {
    return normalizeMessage(response);
  }

  if (
    typeof response === 'object' &&
    response !== null &&
    'message' in response &&
    typeof response.message === 'string'
  ) {
    return normalizeMessage(response.message);
  }

  return undefined;
}

function normalizeMessage(message: string): string | undefined {
  const normalized = message.replace(SERVER_FAILURE_PREFIX, '').trim();
  return normalized.length > 0 ? normalized : undefined;
}
