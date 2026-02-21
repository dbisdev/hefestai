import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import request from 'supertest';
import jwt from 'jsonwebtoken';
import { createApp } from '../../app.js';

vi.mock('../../config/genkit.js', () => ({
  ai: {
    defineFlow: vi.fn(() => vi.fn()),
    generate: vi.fn(),
    embed: vi.fn(),
  },
  googleAI: {},
  geminiModel: 'mocked-model',
  geminiImageModel: 'mocked-image-model',
  embeddingModel: 'mocked-embedding-model',
}));

vi.mock('../../features/generate/generate.flow.js', () => ({
  generateTextFlow: vi.fn(async (input: any) => ({
    text: `Generated: ${input.prompt}`,
    usage: { promptTokens: 10, completionTokens: 20, totalTokens: 30 },
  })),
}));

vi.mock('../../features/chat/chat.flow.js', () => ({
  chatFlow: vi.fn(async (input: any) => ({
    message: 'Chat response',
    usage: { promptTokens: 15, completionTokens: 25, totalTokens: 40 },
  })),
}));

vi.mock('../../features/summarize/summarize.flow.js', () => ({
  summarizeFlow: vi.fn(async (input: any) => ({
    summary: 'Summarized text',
    originalLength: input.text.length,
    summaryLength: 17,
    compressionRatio: 2.5,
    usage: { promptTokens: 50, completionTokens: 10, totalTokens: 60 },
  })),
}));

vi.mock('../../features/embeddings/embeddings.flow.js', () => ({
  embeddingsFlow: vi.fn(async (input: any) => ({
    embeddings: input.texts.map(() => [0.1, 0.2, 0.3]),
    model: 'gemini-embedding-001',
    dimensions: 3,
  })),
}));

vi.mock('../../features/rag-generate/rag-generate.flow.js', () => ({
  ragGenerateFlow: vi.fn(async (input: any) => ({
    answer: `Answer for: ${input.query}`,
    usage: { promptTokens: 100, completionTokens: 50, totalTokens: 150 },
  })),
}));

vi.mock('../../features/generate-image/generate-image.flow.js', () => ({
  imageGenerateFlow: vi.fn(async (input: any) => ({
    image: { base64: 'mocked-base64-data', mimeType: 'image/png' },
    success: true,
    usedPrompt: input.prompt,
  })),
}));

const JWT_SECRET = process.env.JWT_SECRET || 'test-secret-key';
const JWT_ISSUER = 'Loremaster.Api';
const JWT_AUDIENCE = 'Loremaster.Genkit';

function generateTestToken(scopes = ['genkit.execute']): string {
  return jwt.sign(
    {
      sub: 'test-service-id',
      iss: JWT_ISSUER,
      aud: JWT_AUDIENCE,
      scope: scopes.join(' '),
    },
    JWT_SECRET,
    { expiresIn: '1h' }
  );
}

describe('Endpoints with Authentication', () => {
  let app: ReturnType<typeof createApp>;
  let validToken: string;

  beforeEach(() => {
    vi.clearAllMocks();
    process.env.JWT_SECRET = JWT_SECRET;
    process.env.JWT_ISSUER = JWT_ISSUER;
    process.env.JWT_AUDIENCE = JWT_AUDIENCE;
    app = createApp();
    validToken = generateTestToken();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('POST /api/generate', () => {
    it('should return generated text with valid token', async () => {
      const response = await request(app)
        .post('/api/generate')
        .set('Authorization', `Bearer ${validToken}`)
        .send({ prompt: 'Hello AI' });

      expect(response.status).toBe(200);
      expect(response.body).toHaveProperty('text');
      expect(response.body.text).toContain('Hello AI');
      expect(response.body).toHaveProperty('usage');
    });

    it('should validate request body', async () => {
      const response = await request(app)
        .post('/api/generate')
        .set('Authorization', `Bearer ${validToken}`)
        .send({ prompt: '' });

      expect(response.status).toBe(400);
      expect(response.body).toHaveProperty('error', 'Validation error');
    });
  });

  describe('POST /api/chat', () => {
    it('should return chat response with valid token', async () => {
      const response = await request(app)
        .post('/api/chat')
        .set('Authorization', `Bearer ${validToken}`)
        .send({
          messages: [
            { role: 'user', content: 'Hello' },
          ],
        });

      expect(response.status).toBe(200);
      expect(response.body).toHaveProperty('message', 'Chat response');
      expect(response.body).toHaveProperty('usage');
    });

    it('should accept context parameter', async () => {
      const response = await request(app)
        .post('/api/chat')
        .set('Authorization', `Bearer ${validToken}`)
        .send({
          messages: [{ role: 'user', content: 'Hi' }],
          context: 'You are a helpful assistant',
        });

      expect(response.status).toBe(200);
    });
  });

  describe('POST /api/summarize', () => {
    it('should return summary with valid token', async () => {
      const longText = 'This is a long text that needs to be summarized for testing purposes.';
      const response = await request(app)
        .post('/api/summarize')
        .set('Authorization', `Bearer ${validToken}`)
        .send({ text: longText });

      expect(response.status).toBe(200);
      expect(response.body).toHaveProperty('summary');
      expect(response.body).toHaveProperty('originalLength');
      expect(response.body).toHaveProperty('summaryLength');
      expect(response.body).toHaveProperty('compressionRatio');
    });

    it('should reject short text', async () => {
      const response = await request(app)
        .post('/api/summarize')
        .set('Authorization', `Bearer ${validToken}`)
        .send({ text: 'short' });

      expect(response.status).toBe(400);
    });
  });

  describe('POST /api/embeddings', () => {
    it('should return embeddings with valid token', async () => {
      const response = await request(app)
        .post('/api/embeddings')
        .set('Authorization', `Bearer ${validToken}`)
        .send({ texts: ['Hello', 'World'] });

      expect(response.status).toBe(200);
      expect(response.body).toHaveProperty('embeddings');
      expect(response.body.embeddings).toHaveLength(2);
      expect(response.body).toHaveProperty('model');
      expect(response.body).toHaveProperty('dimensions');
    });

    it('should require texts array', async () => {
      const response = await request(app)
        .post('/api/embeddings')
        .set('Authorization', `Bearer ${validToken}`)
        .send({});

      expect(response.status).toBe(400);
    });
  });

  describe('POST /api/rag/generate', () => {
    it('should return RAG answer with valid token', async () => {
      const response = await request(app)
        .post('/api/rag/generate')
        .set('Authorization', `Bearer ${validToken}`)
        .send({
          query: 'What is AI?',
          context: ['AI stands for Artificial Intelligence.'],
        });

      expect(response.status).toBe(200);
      expect(response.body).toHaveProperty('answer');
      expect(response.body.answer).toContain('What is AI?');
    });

    it('should require context array', async () => {
      const response = await request(app)
        .post('/api/rag/generate')
        .set('Authorization', `Bearer ${validToken}`)
        .send({ query: 'test' });

      expect(response.status).toBe(400);
    });
  });

  describe('POST /api/generate-image', () => {
    it('should return image with valid token', async () => {
      const response = await request(app)
        .post('/api/generate-image')
        .set('Authorization', `Bearer ${validToken}`)
        .send({
          prompt: 'A sunset',
          style: 'realistic',
          aspectRatio: '16:9',
        });

      expect(response.status).toBe(200);
      expect(response.body).toHaveProperty('image');
      expect(response.body).toHaveProperty('success', true);
    });

    it('should accept minimal request', async () => {
      const response = await request(app)
        .post('/api/generate-image')
        .set('Authorization', `Bearer ${validToken}`)
        .send({ prompt: 'A cat' });

      expect(response.status).toBe(200);
    });
  });
});

describe('Scope Authorization', () => {
  let app: ReturnType<typeof createApp>;

  beforeEach(() => {
    process.env.JWT_SECRET = JWT_SECRET;
    process.env.JWT_ISSUER = JWT_ISSUER;
    process.env.JWT_AUDIENCE = JWT_AUDIENCE;
    app = createApp();
  });

  it('should reject token without genkit.execute scope', async () => {
    const tokenWithoutScope = generateTestToken(['other.scope']);

    const response = await request(app)
      .post('/api/generate')
      .set('Authorization', `Bearer ${tokenWithoutScope}`)
      .send({ prompt: 'test' });

    expect(response.status).toBe(403);
    expect(response.body).toHaveProperty('error', 'Forbidden');
  });

  it('should accept token with genkit.execute scope', async () => {
    const validScopedToken = generateTestToken(['genkit.execute']);

    const response = await request(app)
      .post('/api/generate')
      .set('Authorization', `Bearer ${validScopedToken}`)
      .send({ prompt: 'test' });

    expect(response.status).toBe(200);
  });
});
