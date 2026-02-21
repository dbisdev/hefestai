import { describe, it, expect } from 'vitest';
import { mapUsage } from '../../common/utils/map-usage.js';

describe('mapUsage', () => {
  it('should return undefined when usage is undefined', () => {
    expect(mapUsage(undefined)).toBeUndefined();
  });

  it('should map usage with all fields', () => {
    const usage = { inputTokens: 100, outputTokens: 50 };
    const result = mapUsage(usage);
    
    expect(result).toEqual({
      promptTokens: 100,
      completionTokens: 50,
      totalTokens: 150,
    });
  });

  it('should handle zero tokens', () => {
    const usage = { inputTokens: 0, outputTokens: 0 };
    const result = mapUsage(usage);
    
    expect(result).toEqual({
      promptTokens: 0,
      completionTokens: 0,
      totalTokens: 0,
    });
  });

  it('should handle missing inputTokens', () => {
    const usage = { outputTokens: 50 };
    const result = mapUsage(usage);
    
    expect(result).toEqual({
      promptTokens: 0,
      completionTokens: 50,
      totalTokens: 50,
    });
  });

  it('should handle missing outputTokens', () => {
    const usage = { inputTokens: 100 };
    const result = mapUsage(usage);
    
    expect(result).toEqual({
      promptTokens: 100,
      completionTokens: 0,
      totalTokens: 100,
    });
  });
});
