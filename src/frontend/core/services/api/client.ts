/**
 * HTTP API Client
 * Single Responsibility: HTTP request handling with auth and error management
 * OWASP compliant: Handles auth tokens, validates responses
 */

import { API_BASE, API_TIMEOUT } from '@core/config/constants';
import { tokenService } from '@core/services/storage/token.service';
import type { RequestConfig, ApiError } from '@core/types';

/**
 * Custom error class for API errors
 */
export class ApiRequestError extends Error {
  constructor(
    message: string,
    public status: number,
    public code?: string,
    public errors?: Record<string, string[]>
  ) {
    super(message);
    this.name = 'ApiRequestError';
  }
}

/**
 * Refresh token and retry flag to prevent infinite loops
 */
let isRefreshing = false;
let refreshSubscribers: ((token: string) => void)[] = [];

function subscribeTokenRefresh(callback: (token: string) => void) {
  refreshSubscribers.push(callback);
}

function onTokenRefreshed(token: string) {
  refreshSubscribers.forEach(callback => callback(token));
  refreshSubscribers = [];
}

/**
 * Attempt to refresh the authentication token
 */
async function tryRefreshToken(): Promise<boolean> {
  const refreshToken = tokenService.getRefreshToken();
  if (!refreshToken) return false;

  try {
    const response = await fetch(`${API_BASE}/auth/refresh-token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });

    if (response.ok) {
      const data = await response.json();
      tokenService.setTokens(data.accessToken, data.refreshToken);
      onTokenRefreshed(data.accessToken);
      return true;
    }
    return false;
  } catch {
    return false;
  }
}

/**
 * Main API request function with automatic token refresh
 */
export async function apiRequest<T>(
  endpoint: string,
  options: RequestConfig = {}
): Promise<T> {
  const { skipAuth = false, timeout = API_TIMEOUT, ...fetchOptions } = options;
  
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeout);

  try {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...fetchOptions.headers,
    };

    // Add auth header if token exists and not skipped
    if (!skipAuth) {
      const token = tokenService.getAccessToken();
      if (token) {
        (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
      }
    }

    const response = await fetch(`${API_BASE}${endpoint}`, {
      ...fetchOptions,
      headers,
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    // Handle 401 - try to refresh token
    if (response.status === 401 && !skipAuth && tokenService.getRefreshToken()) {
      if (!isRefreshing) {
        isRefreshing = true;
        const refreshed = await tryRefreshToken();
        isRefreshing = false;

        if (refreshed) {
          // Retry the request with new token
          const newToken = tokenService.getAccessToken();
          (headers as Record<string, string>)['Authorization'] = `Bearer ${newToken}`;
          
          const retryResponse = await fetch(`${API_BASE}${endpoint}`, {
            ...fetchOptions,
            headers,
          });

          if (!retryResponse.ok) {
            const error = await parseErrorResponse(retryResponse);
            throw new ApiRequestError(error.message, retryResponse.status, error.code, error.errors);
          }

          if (retryResponse.status === 204) {
            return {} as T;
          }

          return retryResponse.json();
        } else {
          tokenService.clearTokens();
          throw new ApiRequestError('Session expired', 401, 'SESSION_EXPIRED');
        }
      } else {
        // Wait for the refresh to complete
        return new Promise((resolve, reject) => {
          subscribeTokenRefresh(async (newToken) => {
            try {
              (headers as Record<string, string>)['Authorization'] = `Bearer ${newToken}`;
              const retryResponse = await fetch(`${API_BASE}${endpoint}`, {
                ...fetchOptions,
                headers,
              });

              if (!retryResponse.ok) {
                const error = await parseErrorResponse(retryResponse);
                reject(new ApiRequestError(error.message, retryResponse.status, error.code, error.errors));
                return;
              }

              if (retryResponse.status === 204) {
                resolve({} as T);
                return;
              }

              resolve(retryResponse.json());
            } catch (err) {
              reject(err);
            }
          });
        });
      }
    }

    // Handle non-OK responses
    if (!response.ok) {
      const error = await parseErrorResponse(response);
      throw new ApiRequestError(error.message, response.status, error.code, error.errors);
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return {} as T;
    }

    return response.json();
  } catch (error) {
    clearTimeout(timeoutId);
    
    if (error instanceof ApiRequestError) {
      throw error;
    }

    if (error instanceof Error) {
      if (error.name === 'AbortError') {
        throw new ApiRequestError('Request timeout', 408, 'TIMEOUT');
      }
      throw new ApiRequestError(error.message, 0, 'NETWORK_ERROR');
    }

    throw new ApiRequestError('Unknown error', 0, 'UNKNOWN');
  }
}

/**
 * Parse error response body
 */
async function parseErrorResponse(response: Response): Promise<ApiError> {
  try {
    const data = await response.json();
    return {
      message: data.message || data.title || `Request failed with status ${response.status}`,
      code: data.code,
      errors: data.errors,
      status: response.status,
    };
  } catch {
    return {
      message: `Request failed with status ${response.status}`,
      status: response.status,
    };
  }
}

/**
 * Convenience methods for common HTTP verbs
 */
export const httpClient = {
  get: <T>(endpoint: string, options?: RequestConfig) => 
    apiRequest<T>(endpoint, { ...options, method: 'GET' }),
  
  post: <T>(endpoint: string, data?: unknown, options?: RequestConfig) => 
    apiRequest<T>(endpoint, { 
      ...options, 
      method: 'POST', 
      body: data ? JSON.stringify(data) : undefined 
    }),
  
  put: <T>(endpoint: string, data?: unknown, options?: RequestConfig) => 
    apiRequest<T>(endpoint, { 
      ...options, 
      method: 'PUT', 
      body: data ? JSON.stringify(data) : undefined 
    }),
  
  patch: <T>(endpoint: string, data?: unknown, options?: RequestConfig) => 
    apiRequest<T>(endpoint, { 
      ...options, 
      method: 'PATCH', 
      body: data ? JSON.stringify(data) : undefined 
    }),
  
  delete: <T>(endpoint: string, options?: RequestConfig) => 
    apiRequest<T>(endpoint, { ...options, method: 'DELETE' }),
};
