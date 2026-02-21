import { describe, it, expect } from 'vitest';
import { buildEnhancedPrompt, STYLE_PROMPTS_EXPORT, ASPECT_RATIO_HINTS_EXPORT } from '../../common/utils/build-prompt.js';
import type { ImageGenerateRequest } from '../../features/generate-image/generate-image.schema.js';

describe('buildEnhancedPrompt', () => {
  it('should return prompt with only aspect ratio when no other options provided', () => {
    const input: ImageGenerateRequest = { prompt: 'A cat', aspectRatio: '1:1' };
    expect(buildEnhancedPrompt(input)).toBe('A cat, square composition');
  });

  it('should add style enhancement', () => {
    const input: ImageGenerateRequest = {
      prompt: 'A sunset',
      style: 'realistic',
      aspectRatio: '16:9',
    };
    const result = buildEnhancedPrompt(input);
    expect(result).toContain('A sunset');
    expect(result).toContain('photorealistic');
    expect(result).toContain('highly detailed');
  });

  it('should add aspect ratio hint', () => {
    const input: ImageGenerateRequest = {
      prompt: 'A mountain',
      aspectRatio: '16:9',
    };
    const result = buildEnhancedPrompt(input);
    expect(result).toContain('A mountain');
    expect(result).toContain('wide landscape format');
    expect(result).toContain('cinematic');
  });

  it('should add negative prompt', () => {
    const input: ImageGenerateRequest = {
      prompt: 'A portrait',
      negativePrompt: 'blurry, low quality',
      aspectRatio: '1:1',
    };
    const result = buildEnhancedPrompt(input);
    expect(result).toContain('A portrait');
    expect(result).toContain('Avoid: blurry, low quality');
  });

  it('should combine all enhancements', () => {
    const input: ImageGenerateRequest = {
      prompt: 'A dragon',
      style: 'fantasy',
      aspectRatio: '1:1',
      negativePrompt: 'cartoonish',
    };
    const result = buildEnhancedPrompt(input);
    expect(result).toContain('A dragon');
    expect(result).toContain('fantasy art style');
    expect(result).toContain('square composition');
    expect(result).toContain('Avoid: cartoonish');
  });

  describe('style enhancements', () => {
    it('should apply realistic style', () => {
      const input: ImageGenerateRequest = { prompt: 'test', style: 'realistic', aspectRatio: '1:1' };
      expect(buildEnhancedPrompt(input)).toContain('photorealistic');
    });

    it('should apply artistic style', () => {
      const input: ImageGenerateRequest = { prompt: 'test', style: 'artistic', aspectRatio: '1:1' };
      expect(buildEnhancedPrompt(input)).toContain('artistic style');
    });

    it('should apply anime style', () => {
      const input: ImageGenerateRequest = { prompt: 'test', style: 'anime', aspectRatio: '1:1' };
      expect(buildEnhancedPrompt(input)).toContain('anime style');
    });

    it('should apply fantasy style', () => {
      const input: ImageGenerateRequest = { prompt: 'test', style: 'fantasy', aspectRatio: '1:1' };
      expect(buildEnhancedPrompt(input)).toContain('fantasy art style');
    });

    it('should apply sketch style', () => {
      const input: ImageGenerateRequest = { prompt: 'test', style: 'sketch', aspectRatio: '1:1' };
      expect(buildEnhancedPrompt(input)).toContain('pencil sketch style');
    });
  });

  describe('aspect ratio hints', () => {
    it('should apply 1:1 aspect ratio', () => {
      const input: ImageGenerateRequest = { prompt: 'test', aspectRatio: '1:1' };
      expect(buildEnhancedPrompt(input)).toContain('square composition');
    });

    it('should apply 16:9 aspect ratio', () => {
      const input: ImageGenerateRequest = { prompt: 'test', aspectRatio: '16:9' };
      expect(buildEnhancedPrompt(input)).toContain('wide landscape format');
    });

    it('should apply 9:16 aspect ratio', () => {
      const input: ImageGenerateRequest = { prompt: 'test', aspectRatio: '9:16' };
      expect(buildEnhancedPrompt(input)).toContain('vertical portrait format');
    });

    it('should apply 4:3 aspect ratio', () => {
      const input: ImageGenerateRequest = { prompt: 'test', aspectRatio: '4:3' };
      expect(buildEnhancedPrompt(input)).toContain('standard landscape format');
    });

    it('should apply 3:4 aspect ratio', () => {
      const input: ImageGenerateRequest = { prompt: 'test', aspectRatio: '3:4' };
      expect(buildEnhancedPrompt(input)).toContain('standard portrait format');
    });
  });

  describe('STYLE_PROMPTS constant', () => {
    it('should have all required styles', () => {
      expect(STYLE_PROMPTS_EXPORT).toHaveProperty('realistic');
      expect(STYLE_PROMPTS_EXPORT).toHaveProperty('artistic');
      expect(STYLE_PROMPTS_EXPORT).toHaveProperty('anime');
      expect(STYLE_PROMPTS_EXPORT).toHaveProperty('fantasy');
      expect(STYLE_PROMPTS_EXPORT).toHaveProperty('sketch');
    });

    it('should have non-empty values', () => {
      Object.values(STYLE_PROMPTS_EXPORT).forEach(value => {
        expect(value.length).toBeGreaterThan(0);
      });
    });
  });

  describe('ASPECT_RATIO_HINTS constant', () => {
    it('should have all required aspect ratios', () => {
      expect(ASPECT_RATIO_HINTS_EXPORT).toHaveProperty('1:1');
      expect(ASPECT_RATIO_HINTS_EXPORT).toHaveProperty('16:9');
      expect(ASPECT_RATIO_HINTS_EXPORT).toHaveProperty('9:16');
      expect(ASPECT_RATIO_HINTS_EXPORT).toHaveProperty('4:3');
      expect(ASPECT_RATIO_HINTS_EXPORT).toHaveProperty('3:4');
    });

    it('should have non-empty values', () => {
      Object.values(ASPECT_RATIO_HINTS_EXPORT).forEach(value => {
        expect(value.length).toBeGreaterThan(0);
      });
    });
  });
});
