/**
 * HTTP API Client Unit Tests
 * Tests for HTTP request handling with auth and error management
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

const mockGetAccessToken = vi.fn();
const mockGetRefreshToken = vi.fn();
const mockSetTokens = vi.fn();
const mockClearTokens = vi.fn();
const mockGetTokenExpiration = vi.fn();

vi.mock('@core/services/storage/token.service', () => ({
  tokenService: {
    getAccessToken: () => mockGetAccessToken(),
    getRefreshToken: () => mockGetRefreshToken(),
    setTokens: mockSetTokens,
    clearTokens: mockClearTokens,
    getTokenExpiration: mockGetTokenExpiration,
  },
}));

vi.stubGlobal('fetch', vi.fn());

describe('HTTP Client', () => {
  let fetchMock: ReturnType<typeof vi.fn>;
  
  beforeEach(async () => {
    vi.clearAllMocks();
    mockGetAccessToken.mockReturnValue('test-access-token');
    mockGetRefreshToken.mockReturnValue(null);
    mockGetTokenExpiration.mockReturnValue(new Date(Date.now() + 3600000));
    vi.resetModules();
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('ApiRequestError', () => {
    it('creates error with message and status', async () => {
      const { ApiRequestError } = await import('./client');
      const error = new ApiRequestError('Test error', 400);
      
      expect(error.message).toBe('Test error');
      expect(error.status).toBe(400);
      expect(error.name).toBe('ApiRequestError');
    });

    it('creates error with code and errors', async () => {
      const { ApiRequestError } = await import('./client');
      const errors = { field: ['error1', 'error2'] };
      const error = new ApiRequestError('Validation error', 400, 'VALIDATION_ERROR', errors);
      
      expect(error.code).toBe('VALIDATION_ERROR');
      expect(error.errors).toEqual(errors);
    });
  });

  describe('apiRequest', () => {
    it('makes GET request with auth header', async () => {
      const { apiRequest } = await import('./client');
      fetchMock.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ data: 'test' }),
      });

      const result = await apiRequest('/test');
      
      expect(fetchMock).toHaveBeenCalled();
      const [url, options] = fetchMock.mock.calls[0];
      expect(url).toContain('/api/test');
      // GET is the default method in fetch, so method may be undefined or 'GET'
      expect(options.method === undefined || options.method === 'GET').toBe(true);
      expect(options.headers['Authorization']).toBe('Bearer test-access-token');
      expect(result).toEqual({ data: 'test' });
    });

    it('skips auth header when skipAuth is true', async () => {
      const { apiRequest } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ data: 'test' }),
      });

      await apiRequest('/test', { skipAuth: true });
      
      const callArgs = fetch.mock.calls[0][1];
      expect(callArgs.headers).not.toHaveProperty('Authorization');
    });

    it('makes POST request with body', async () => {
      const { apiRequest } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: true,
        status: 201,
        json: async () => ({ id: 1 }),
      });

      const result = await apiRequest('/test', {
        method: 'POST',
        body: JSON.stringify({ name: 'test' }),
      });
      
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/test'),
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ name: 'test' }),
        })
      );
      expect(result).toEqual({ id: 1 });
    });

    it('handles 204 No Content response', async () => {
      const { apiRequest } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: true,
        status: 204,
      });

      const result = await apiRequest('/test');
      
      expect(result).toEqual({});
    });

    it('throws ApiRequestError for non-OK responses', async () => {
      const { apiRequest, ApiRequestError } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        json: async () => ({ message: 'Not found' }),
      });

      await expect(apiRequest('/test')).rejects.toThrow(ApiRequestError);
    });

    it('extracts error code from response', async () => {
      const { apiRequest } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ message: 'Bad request', code: 'INVALID_INPUT' }),
      });

      try {
        await apiRequest('/test');
      } catch (error: unknown) {
        expect((error as { code: string }).code).toBe('INVALID_INPUT');
      }
    });

    it('handles network errors', async () => {
      const { apiRequest } = await import('./client');
      fetch.mockRejectedValueOnce(new Error('Network error'));

      await expect(apiRequest('/test')).rejects.toMatchObject({
        code: 'NETWORK_ERROR',
        status: 0,
      });
    });

    it('handles timeout errors', async () => {
      const { apiRequest } = await import('./client');
      const abortError = new Error('Aborted');
      abortError.name = 'AbortError';
      fetch.mockRejectedValueOnce(abortError);

      await expect(apiRequest('/test', { timeout: 1000 })).rejects.toMatchObject({
        code: 'TIMEOUT',
        status: 408,
      });
    });

    it('attempts token refresh on 401', async () => {
      mockGetRefreshToken.mockReturnValue('refresh-token');
      
      fetch
        .mockResolvedValueOnce({
          ok: false,
          status: 401,
          json: async () => ({ message: 'Unauthorized' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ accessToken: 'new-token', refreshToken: 'new-refresh' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          status: 200,
          json: async () => ({ data: 'success' }),
        });

      const { apiRequest } = await import('./client');
      const result = await apiRequest('/test');
      
      expect(mockSetTokens).toHaveBeenCalledWith('new-token', 'new-refresh');
      expect(result).toEqual({ data: 'success' });
    });

    it('clears tokens when refresh fails', async () => {
      mockGetRefreshToken.mockReturnValue('refresh-token');
      
      fetch
        .mockResolvedValueOnce({
          ok: false,
          status: 401,
          json: async () => ({ message: 'Unauthorized' }),
        })
        .mockResolvedValueOnce({
          ok: false,
          status: 401,
        });

      const { apiRequest } = await import('./client');
      await expect(apiRequest('/test')).rejects.toMatchObject({
        code: 'SESSION_EXPIRED',
        status: 401,
      });
      
      expect(mockClearTokens).toHaveBeenCalled();
    });

    it('does not attempt refresh without refresh token', async () => {
      mockGetRefreshToken.mockReturnValue(null);
      
      fetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: async () => ({ message: 'Unauthorized' }),
      });

      const { apiRequest } = await import('./client');
      await expect(apiRequest('/test')).rejects.toMatchObject({
        status: 401,
      });
    });
  });

  describe('httpClient convenience methods', () => {
    it('get makes GET request', async () => {
      const { httpClient } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ data: 'test' }),
      });

      await httpClient.get('/test');
      
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/test'),
        expect.objectContaining({ method: 'GET' })
      );
    });

    it('post makes POST request with data', async () => {
      const { httpClient } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: true,
        status: 201,
        json: async () => ({ id: 1 }),
      });

      await httpClient.post('/test', { name: 'test' });
      
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/test'),
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ name: 'test' }),
        })
      );
    });

    it('put makes PUT request with data', async () => {
      const { httpClient } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ updated: true }),
      });

      await httpClient.put('/test/1', { name: 'updated' });
      
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/test/1'),
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify({ name: 'updated' }),
        })
      );
    });

    it('patch makes PATCH request with data', async () => {
      const { httpClient } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ patched: true }),
      });

      await httpClient.patch('/test/1', { status: 'active' });
      
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/test/1'),
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify({ status: 'active' }),
        })
      );
    });

    it('delete makes DELETE request', async () => {
      const { httpClient } = await import('./client');
      fetch.mockResolvedValueOnce({
        ok: true,
        status: 204,
      });

      await httpClient.delete('/test/1');
      
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/test/1'),
        expect.objectContaining({ method: 'DELETE' })
      );
    });
  });
});
