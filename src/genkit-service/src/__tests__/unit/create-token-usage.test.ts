import { describe, it, expect } from 'vitest';
import { createTokenUsage } from '../../common/types/usage.js';

describe('createTokenUsage', () => {
  it('should create usage with all fields', () => {
    const result = createTokenUsage(100, 50);
    
    expect(result).toEqual({
      promptTokens: 100,
      completionTokens: 50,
      totalTokens: 150,
    });
  });

  it('should handle undefined inputTokens', () => {
    const result = createTokenUsage(undefined, 50);
    
    expect(result).toEqual({
      promptTokens: 0,
      completionTokens: 50,
      totalTokens: 50,
    });
  });

  it('should handle undefined outputTokens', () => {
    const result = createTokenUsage(100, undefined);
    
    expect(result).toEqual({
      promptTokens: 100,
      completionTokens: 0,
      totalTokens: 100,
    });
  });

  it('should handle both undefined', () => {
    const result = createTokenUsage(undefined, undefined);
    
    expect(result).toEqual({
      promptTokens: 0,
      completionTokens: 0,
      totalTokens: 0,
    });
  });

  it('should calculate totalTokens correctly', () => {
    expect(createTokenUsage(1000, 500).totalTokens).toBe(1500);
    expect(createTokenUsage(0, 0).totalTokens).toBe(0);
    expect(createTokenUsage(10, 20).totalTokens).toBe(30);
  });
});
