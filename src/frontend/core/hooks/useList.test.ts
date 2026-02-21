/**
 * useList Hook Tests
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { useList } from './useList';

interface TestItem {
  id: string;
  name: string;
  description: string;
  count: number;
}

const mockItems: TestItem[] = [
  { id: '1', name: 'Alpha', description: 'First item', count: 10 },
  { id: '2', name: 'Beta', description: 'Second item', count: 5 },
  { id: '3', name: 'Gamma', description: 'Third item', count: 15 },
];

describe('useList', () => {
  let mockFetchFn: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    mockFetchFn = vi.fn().mockResolvedValue(mockItems);
  });

  it('fetches items on mount', async () => {
    const { result } = renderHook(() =>
      useList({ fetchFn: mockFetchFn })
    );

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.items).toEqual(mockItems);
    expect(mockFetchFn).toHaveBeenCalledTimes(1);
  });

  it('handles fetch errors', async () => {
    const errorFn = vi.fn().mockRejectedValue(new Error('Fetch failed'));
    
    const { result } = renderHook(() =>
      useList({ fetchFn: errorFn })
    );

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.error).toBe('Fetch failed');
    expect(result.current.items).toEqual([]);
  });

  it('filters items by search term', async () => {
    const { result } = renderHook(() =>
      useList({
        fetchFn: mockFetchFn,
        searchFields: ['name', 'description'],
      })
    );

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    act(() => {
      result.current.setSearchTerm('Alpha');
    });

    expect(result.current.filteredItems).toHaveLength(1);
    expect(result.current.filteredItems[0].name).toBe('Alpha');
  });

  it('returns all items when search is empty', async () => {
    const { result } = renderHook(() =>
      useList({
        fetchFn: mockFetchFn,
        searchFields: ['name'],
      })
    );

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.filteredItems).toHaveLength(3);
  });

  it('sorts items ascending', async () => {
    const { result } = renderHook(() =>
      useList({ fetchFn: mockFetchFn })
    );

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    act(() => {
      result.current.setSortBy('name');
    });

    expect(result.current.filteredItems[0].name).toBe('Alpha');
    expect(result.current.filteredItems[2].name).toBe('Gamma');
  });

  it('sorts items descending on second click', async () => {
    const { result } = renderHook(() =>
      useList({ fetchFn: mockFetchFn })
    );

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    act(() => {
      result.current.setSortBy('name');
    });

    act(() => {
      result.current.setSortBy('name');
    });

    expect(result.current.filteredItems[0].name).toBe('Gamma');
    expect(result.current.filteredItems[2].name).toBe('Alpha');
  });

  it('sorts by numeric field', async () => {
    const { result } = renderHook(() =>
      useList({ fetchFn: mockFetchFn })
    );

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    act(() => {
      result.current.setSortBy('count');
    });

    expect(result.current.filteredItems[0].count).toBe(5);
    expect(result.current.filteredItems[2].count).toBe(15);
  });

  it('refreshes items', async () => {
    const { result } = renderHook(() =>
      useList({ fetchFn: mockFetchFn })
    );

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.refresh();
    });

    expect(mockFetchFn).toHaveBeenCalledTimes(2);
  });

  it('does not fetch when disabled', async () => {
    const { result } = renderHook(() =>
      useList({ fetchFn: mockFetchFn, enabled: false })
    );

    expect(result.current.items).toEqual([]);
    expect(mockFetchFn).not.toHaveBeenCalled();
  });
});
