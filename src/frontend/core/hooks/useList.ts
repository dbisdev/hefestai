/**
 * useList Hook
 * Single Responsibility: Generic list fetching with search and sorting
 * DRY: Reusable across all pages that need list functionality
 * OCP: Extends useApi without modifying it
 */

import { useState, useEffect, useCallback, useMemo } from 'react';

interface UseListConfig<T> {
  fetchFn: () => Promise<T[]>;
  searchFields?: (keyof T)[];
  enabled?: boolean;
}

interface UseListReturn<T> {
  items: T[];
  filteredItems: T[];
  isLoading: boolean;
  error: string | null;
  searchTerm: string;
  setSearchTerm: (term: string) => void;
  sortBy: keyof T | null;
  sortDirection: 'asc' | 'desc';
  setSortBy: (field: keyof T) => void;
  refresh: () => Promise<void>;
}

export function useList<T>({
  fetchFn,
  searchFields = [],
  enabled = true,
}: UseListConfig<T>): UseListReturn<T> {
  const [items, setItems] = useState<T[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortBy, setSortBy] = useState<keyof T | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');

  const refresh = useCallback(async () => {
    if (!enabled) {
      setItems([]);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const data = await fetchFn();
      setItems(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error loading data';
      setError(message);
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  }, [fetchFn, enabled]);

  useEffect(() => {
    if (!enabled) return;
    refresh();
  }, [enabled, refresh]);

  const filteredItems = useMemo(() => {
    let result = [...items];

    if (searchTerm.trim() && searchFields.length > 0) {
      const normalizedSearch = searchTerm.toLowerCase().trim();
      result = result.filter((item) =>
        searchFields.some((field) => {
          const value = item[field];
          if (typeof value === 'string') {
            return value.toLowerCase().includes(normalizedSearch);
          }
          return false;
        })
      );
    }

    if (sortBy) {
      result.sort((a, b) => {
        const aVal = a[sortBy];
        const bVal = b[sortBy];

        if (aVal === bVal) return 0;
        if (aVal === null || aVal === undefined) return 1;
        if (bVal === null || bVal === undefined) return -1;

        let comparison = 0;
        if (typeof aVal === 'string' && typeof bVal === 'string') {
          comparison = aVal.localeCompare(bVal);
        } else if (typeof aVal === 'number' && typeof bVal === 'number') {
          comparison = aVal - bVal;
        }

        return sortDirection === 'asc' ? comparison : -comparison;
      });
    }

    return result;
  }, [items, searchTerm, searchFields, sortBy, sortDirection]);

  const handleSetSortBy = useCallback((field: keyof T) => {
    setSortBy((current) => {
      if (current === field) {
        setSortDirection((dir) => (dir === 'asc' ? 'desc' : 'asc'));
        return current;
      }
      setSortDirection('asc');
      return field;
    });
  }, []);

  return {
    items,
    filteredItems,
    isLoading,
    error,
    searchTerm,
    setSearchTerm,
    sortBy,
    sortDirection,
    setSortBy: handleSetSortBy,
    refresh,
  };
}
