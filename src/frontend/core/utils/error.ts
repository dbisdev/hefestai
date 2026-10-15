/**
 * Error utility helpers
 *
 * Provides a single, consistent way to extract a human-readable message from
 * any caught value, regardless of whether it is an Error instance, a plain
 * string, or an unknown object.  Centralises the ~25 inline duplicates of the
 * pattern `err instanceof Error ? err.message : 'fallback'` that were spread
 * across hooks and pages.
 */

import { ApiRequestError } from '@core/services/api/client';

/**
 * Extracts a user-readable error message from any thrown value.
 *
 * Priority:
 *  1. `ApiRequestError.message`  (already human-readable from the server)
 *  2. `Error.message`            (standard JS Error)
 *  3. The value itself if it is a non-empty string
 *  4. The supplied `fallback`    (defaults to 'An unexpected error occurred')
 *
 * @param err      - The caught value (unknown type).
 * @param fallback - Message to return when nothing better is available.
 * @returns        A non-empty string suitable for display in the UI.
 */
export function extractErrorMessage(
  err: unknown,
  fallback = 'An unexpected error occurred'
): string {
  if (err instanceof ApiRequestError) return err.message;
  if (err instanceof Error) return err.message;
  if (typeof err === 'string' && err.length > 0) return err;
  return fallback;
}

/**
 * Returns true when the error originated from a failed HTTP request.
 *
 * Useful to distinguish network/server failures from local business-logic
 * errors so callers can decide whether to retry or show a different message.
 *
 * @param err - The caught value.
 */
export function isApiError(err: unknown): err is ApiRequestError {
  return err instanceof ApiRequestError;
}

/**
 * Returns true when the error represents an authentication failure (HTTP 401).
 *
 * @param err - The caught value.
 */
export function isAuthError(err: unknown): boolean {
  return err instanceof ApiRequestError && err.status === 401;
}

/**
 * Returns true when the error represents a network connectivity problem,
 * i.e. the request never reached the server (status 0 or no status).
 *
 * @param err - The caught value.
 */
export function isNetworkError(err: unknown): boolean {
  return err instanceof ApiRequestError && err.status === 0;
}
