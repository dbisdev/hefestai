/**
 * API Hook for data fetching with loading/error states
 * Single Responsibility: Async data fetching state management
 */

import { useState, useCallback } from 'react';

interface UseApiState<T> {
  data: T | null;
  isLoading: boolean;
  error: string | null;
}

interface UseApiResult<T, P extends unknown[]> extends UseApiState<T> {
  execute: (...params: P) => Promise<T | null>;
  reset: () => void;
  setData: (data: T | null) => void;
}

/**
 * Hook for managing API calls with loading and error states
 * @param apiFunc The async function to execute
 */
export function useApi<T, P extends unknown[] = []>(
  apiFunc: (...params: P) => Promise<T>
): UseApiResult<T, P> {
  const [state, setState] = useState<UseApiState<T>>({
    data: null,
    isLoading: false,
    error: null,
  });

  const execute = useCallback(async (...params: P): Promise<T | null> => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));
    
    try {
      const result = await apiFunc(...params);
      setState({ data: result, isLoading: false, error: null });
      return result;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'An error occurred';
      setState(prev => ({ ...prev, isLoading: false, error: message }));
      return null;
    }
  }, [apiFunc]);

  const reset = useCallback(() => {
    setState({ data: null, isLoading: false, error: null });
  }, []);

  const setData = useCallback((data: T | null) => {
    setState(prev => ({ ...prev, data }));
  }, []);

  return {
    ...state,
    execute,
    reset,
    setData,
  };
}

/**
 * Hook for mutation operations (create, update, delete)
 */
export function useMutation<T, P extends unknown[] = []>(
  mutationFunc: (...params: P) => Promise<T>
) {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(async (...params: P): Promise<T | null> => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await mutationFunc(...params);
      setIsLoading(false);
      return result;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'An error occurred';
      setError(message);
      setIsLoading(false);
      return null;
    }
  }, [mutationFunc]);

  const reset = useCallback(() => {
    setError(null);
  }, []);

  return {
    mutate,
    isLoading,
    error,
    reset,
  };
}
