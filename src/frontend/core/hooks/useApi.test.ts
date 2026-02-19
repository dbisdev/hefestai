/**
 * useApi Hook Unit Tests
 * Tests for API data fetching with loading/error states
 */
import { describe, it, expect, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useApi, useMutation } from './useApi';

describe('useApi Hook', () => {
  describe('useApi', () => {
    it('initializes with default state', () => {
      const { result } = renderHook(() => useApi(async () => 'test'));
      
      expect(result.current.data).toBeNull();
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('sets loading state during execution', async () => {
      const mockApiFunc = vi.fn().mockImplementation(() => 
        new Promise(resolve => setTimeout(() => resolve('data'), 100))
      );
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      act(() => {
        result.current.execute();
      });
      
      expect(result.current.isLoading).toBe(true);
    });

    it('sets data on successful execution', async () => {
      const mockApiFunc = vi.fn().mockResolvedValue('test data');
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      await act(async () => {
        await result.current.execute();
      });
      
      expect(result.current.data).toBe('test data');
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('sets error on failed execution', async () => {
      const mockApiFunc = vi.fn().mockRejectedValue(new Error('API error'));
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      await act(async () => {
        await result.current.execute();
      });
      
      expect(result.current.data).toBeNull();
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBe('API error');
    });

    it('handles non-Error rejections', async () => {
      const mockApiFunc = vi.fn().mockRejectedValue('string error');
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      await act(async () => {
        await result.current.execute();
      });
      
      expect(result.current.error).toBe('An error occurred');
    });

    it('passes parameters to API function', async () => {
      const mockApiFunc = vi.fn().mockResolvedValue('result');
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      await act(async () => {
        await result.current.execute('param1', 'param2');
      });
      
      expect(mockApiFunc).toHaveBeenCalledWith('param1', 'param2');
    });

    it('returns result from execute', async () => {
      const mockApiFunc = vi.fn().mockResolvedValue('result');
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      let executeResult: unknown = null;
      await act(async () => {
        executeResult = await result.current.execute();
      });
      
      expect(executeResult).toBe('result');
    });

    it('returns null from execute on error', async () => {
      const mockApiFunc = vi.fn().mockRejectedValue(new Error('error'));
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      let executeResult: unknown = 'not null';
      await act(async () => {
        executeResult = await result.current.execute();
      });
      
      expect(executeResult).toBeNull();
    });

    it('resets state with reset function', async () => {
      const mockApiFunc = vi.fn().mockResolvedValue('data');
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      await act(async () => {
        await result.current.execute();
      });
      
      expect(result.current.data).toBe('data');
      
      act(() => {
        result.current.reset();
      });
      
      expect(result.current.data).toBeNull();
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('updates data with setData function', async () => {
      const mockApiFunc = vi.fn().mockResolvedValue('initial');
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      await act(async () => {
        await result.current.execute();
      });
      
      act(() => {
        result.current.setData('updated data');
      });
      
      expect(result.current.data).toBe('updated data');
    });

    it('can set data to null', async () => {
      const mockApiFunc = vi.fn().mockResolvedValue('data');
      
      const { result } = renderHook(() => useApi(mockApiFunc));
      
      await act(async () => {
        await result.current.execute();
      });
      
      act(() => {
        result.current.setData(null);
      });
      
      expect(result.current.data).toBeNull();
    });
  });

  describe('useMutation', () => {
    it('initializes with default state', () => {
      const { result } = renderHook(() => useMutation(async () => 'test'));
      
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('sets loading state during mutation', async () => {
      const mockMutateFunc = vi.fn().mockImplementation(() => 
        new Promise(resolve => setTimeout(() => resolve('data'), 100))
      );
      
      const { result } = renderHook(() => useMutation(mockMutateFunc));
      
      act(() => {
        result.current.mutate();
      });
      
      expect(result.current.isLoading).toBe(true);
    });

    it('returns result on successful mutation', async () => {
      const mockMutateFunc = vi.fn().mockResolvedValue('result');
      
      const { result } = renderHook(() => useMutation(mockMutateFunc));
      
      let mutateResult: unknown = null;
      await act(async () => {
        mutateResult = await result.current.mutate('arg1', 'arg2');
      });
      
      expect(mutateResult).toBe('result');
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
      expect(mockMutateFunc).toHaveBeenCalledWith('arg1', 'arg2');
    });

    it('sets error on failed mutation', async () => {
      const mockMutateFunc = vi.fn().mockRejectedValue(new Error('Mutation failed'));
      
      const { result } = renderHook(() => useMutation(mockMutateFunc));
      
      await act(async () => {
        await result.current.mutate();
      });
      
      expect(result.current.error).toBe('Mutation failed');
      expect(result.current.isLoading).toBe(false);
    });

    it('returns null on failed mutation', async () => {
      const mockMutateFunc = vi.fn().mockRejectedValue(new Error('error'));
      
      const { result } = renderHook(() => useMutation(mockMutateFunc));
      
      let mutateResult: unknown = 'not null';
      await act(async () => {
        mutateResult = await result.current.mutate();
      });
      
      expect(mutateResult).toBeNull();
    });

    it('resets error with reset function', async () => {
      const mockMutateFunc = vi.fn().mockRejectedValue(new Error('error'));
      
      const { result } = renderHook(() => useMutation(mockMutateFunc));
      
      await act(async () => {
        await result.current.mutate();
      });
      
      expect(result.current.error).toBe('error');
      
      act(() => {
        result.current.reset();
      });
      
      expect(result.current.error).toBeNull();
    });
  });
});
