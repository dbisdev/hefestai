import { describe, it, expect } from 'vitest';
import { TextGenerationRequestSchema, TextGenerationResponseSchema } from '../../features/generate/generate.schema.js';
import { ChatRequestSchema, ChatResponseSchema, ChatMessageSchema } from '../../features/chat/chat.schema.js';
import { SummarizeRequestSchema, SummarizeResponseSchema } from '../../features/summarize/summarize.schema.js';
import { EmbeddingsRequestSchema, EmbeddingsResponseSchema } from '../../features/embeddings/embeddings.schema.js';
import { RagGenerateRequestSchema, RagGenerateResponseSchema } from '../../features/rag-generate/rag-generate.schema.js';
import { ImageGenerateRequestSchema, ImageGenerateResponseSchema } from '../../features/generate-image/generate-image.schema.js';

describe('TextGeneration Schema Validation', () => {
  it('should validate valid request', () => {
    const result = TextGenerationRequestSchema.safeParse({
      prompt: 'Hello world',
      systemPrompt: 'You are helpful',
      temperature: 0.7,
      maxTokens: 2048,
    });
    expect(result.success).toBe(true);
  });

  it('should apply default values', () => {
    const result = TextGenerationRequestSchema.safeParse({ prompt: 'test' });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.temperature).toBe(0.7);
      expect(result.data.maxTokens).toBe(2048);
    }
  });

  it('should reject empty prompt', () => {
    const result = TextGenerationRequestSchema.safeParse({ prompt: '' });
    expect(result.success).toBe(false);
  });

  it('should reject prompt over 10000 chars', () => {
    const result = TextGenerationRequestSchema.safeParse({ prompt: 'a'.repeat(10001) });
    expect(result.success).toBe(false);
  });

  it('should reject temperature over 2', () => {
    const result = TextGenerationRequestSchema.safeParse({ prompt: 'test', temperature: 3 });
    expect(result.success).toBe(false);
  });

  it('should reject negative temperature', () => {
    const result = TextGenerationRequestSchema.safeParse({ prompt: 'test', temperature: -0.5 });
    expect(result.success).toBe(false);
  });

  it('should validate response schema', () => {
    const result = TextGenerationResponseSchema.safeParse({
      text: 'Generated text',
      usage: { promptTokens: 10, completionTokens: 20, totalTokens: 30 },
    });
    expect(result.success).toBe(true);
  });
});

describe('Chat Schema Validation', () => {
  it('should validate valid chat request', () => {
    const result = ChatRequestSchema.safeParse({
      messages: [
        { role: 'user', content: 'Hello' },
        { role: 'assistant', content: 'Hi there' },
      ],
      context: 'System context',
      temperature: 0.5,
      maxTokens: 1024,
    });
    expect(result.success).toBe(true);
  });

  it('should reject invalid role', () => {
    const result = ChatMessageSchema.safeParse({ role: 'invalid', content: 'test' });
    expect(result.success).toBe(false);
  });

  it('should accept system role', () => {
    const result = ChatMessageSchema.safeParse({ role: 'system', content: 'system message' });
    expect(result.success).toBe(true);
  });

  it('should reject empty messages array', () => {
    const result = ChatRequestSchema.safeParse({ messages: [] });
    expect(result.success).toBe(true);
  });
});

describe('Summarize Schema Validation', () => {
  it('should validate valid summarize request', () => {
    const result = SummarizeRequestSchema.safeParse({
      text: 'This is a long text that needs to be summarized for the user.',
      style: 'concise',
      maxLength: 200,
      language: 'en',
    });
    expect(result.success).toBe(true);
  });

  it('should reject text under 10 chars', () => {
    const result = SummarizeRequestSchema.safeParse({ text: 'short' });
    expect(result.success).toBe(false);
  });

  it('should reject text over 50000 chars', () => {
    const result = SummarizeRequestSchema.safeParse({ text: 'a'.repeat(50001) });
    expect(result.success).toBe(false);
  });

  it('should reject invalid style', () => {
    const result = SummarizeRequestSchema.safeParse({
      text: 'This is long enough text',
      style: 'invalid',
    });
    expect(result.success).toBe(false);
  });

  it('should accept all valid styles', () => {
    const styles = ['concise', 'detailed', 'bullet-points'] as const;
    for (const style of styles) {
      const result = SummarizeRequestSchema.safeParse({
        text: 'This is a valid text for summarization',
        style,
      });
      expect(result.success).toBe(true);
    }
  });
});

describe('Embeddings Schema Validation', () => {
  it('should validate valid embeddings request', () => {
    const result = EmbeddingsRequestSchema.safeParse({
      texts: ['Hello world', 'Another text'],
      model: 'gemini-embedding-001',
    });
    expect(result.success).toBe(true);
  });

  it('should reject empty texts array', () => {
    const result = EmbeddingsRequestSchema.safeParse({ texts: [] });
    expect(result.success).toBe(false);
  });

  it('should reject over 100 texts', () => {
    const texts = Array(101).fill('text');
    const result = EmbeddingsRequestSchema.safeParse({ texts });
    expect(result.success).toBe(false);
  });

  it('should reject empty string in texts', () => {
    const result = EmbeddingsRequestSchema.safeParse({ texts: ['valid', ''] });
    expect(result.success).toBe(false);
  });

  it('should apply default model', () => {
    const result = EmbeddingsRequestSchema.safeParse({ texts: ['test'] });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.model).toBe('gemini-embedding-001');
    }
  });
});

describe('RAG Generate Schema Validation', () => {
  it('should validate valid RAG request', () => {
    const result = RagGenerateRequestSchema.safeParse({
      query: 'What is the capital of France?',
      context: ['France is a country in Europe.', 'Paris is the capital of France.'],
      systemPrompt: 'Answer based on context',
      temperature: 0.3,
      maxTokens: 1024,
    });
    expect(result.success).toBe(true);
  });

  it('should reject empty context array', () => {
    const result = RagGenerateRequestSchema.safeParse({
      query: 'test query',
      context: [],
    });
    expect(result.success).toBe(false);
  });

  it('should reject over 10 context items', () => {
    const context = Array(11).fill('context item');
    const result = RagGenerateRequestSchema.safeParse({
      query: 'test',
      context,
    });
    expect(result.success).toBe(false);
  });

  it('should reject empty query', () => {
    const result = RagGenerateRequestSchema.safeParse({
      query: '',
      context: ['some context'],
    });
    expect(result.success).toBe(false);
  });
});

describe('Image Generate Schema Validation', () => {
  it('should validate valid image request', () => {
    const result = ImageGenerateRequestSchema.safeParse({
      prompt: 'A beautiful sunset over mountains',
      negativePrompt: 'blurry, low quality',
      aspectRatio: '16:9',
      style: 'realistic',
    });
    expect(result.success).toBe(true);
  });

  it('should reject empty prompt', () => {
    const result = ImageGenerateRequestSchema.safeParse({ prompt: '' });
    expect(result.success).toBe(false);
  });

  it('should reject prompt over 2000 chars', () => {
    const result = ImageGenerateRequestSchema.safeParse({ prompt: 'a'.repeat(2001) });
    expect(result.success).toBe(false);
  });

  it('should reject invalid aspect ratio', () => {
    const result = ImageGenerateRequestSchema.safeParse({
      prompt: 'test',
      aspectRatio: '5:4',
    });
    expect(result.success).toBe(false);
  });

  it('should accept all valid aspect ratios', () => {
    const ratios = ['1:1', '16:9', '9:16', '4:3', '3:4'] as const;
    for (const aspectRatio of ratios) {
      const result = ImageGenerateRequestSchema.safeParse({
        prompt: 'test image',
        aspectRatio,
      });
      expect(result.success).toBe(true);
    }
  });

  it('should accept all valid styles', () => {
    const styles = ['realistic', 'artistic', 'anime', 'fantasy', 'sketch'] as const;
    for (const style of styles) {
      const result = ImageGenerateRequestSchema.safeParse({
        prompt: 'test image',
        style,
      });
      expect(result.success).toBe(true);
    }
  });

  it('should apply default aspect ratio', () => {
    const result = ImageGenerateRequestSchema.safeParse({ prompt: 'test' });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.aspectRatio).toBe('1:1');
    }
  });

  it('should validate response schema', () => {
    const result = ImageGenerateResponseSchema.safeParse({
      image: { base64: 'abc123', mimeType: 'image/png' },
      success: true,
      usedPrompt: 'enhanced prompt',
    });
    expect(result.success).toBe(true);
  });

  it('should accept null image with success false', () => {
    const result = ImageGenerateResponseSchema.safeParse({
      image: null,
      success: false,
      message: 'Generation failed',
    });
    expect(result.success).toBe(true);
  });
});
