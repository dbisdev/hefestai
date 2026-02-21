import { describe, it, expect } from 'vitest';
import request from 'supertest';
import { createApp } from '../../app.js';

const app = createApp();

describe('Authentication Middleware', () => {
  it('should reject requests without authorization header on /api/generate', async () => {
    const response = await request(app).post('/api/generate');
    expect(response.status).toBe(401);
    expect(response.body).toHaveProperty('error', 'Unauthorized');
  });

  it('should reject requests without authorization header on /api/chat', async () => {
    const response = await request(app).post('/api/chat');
    expect(response.status).toBe(401);
  });

  it('should reject requests without authorization header on /api/summarize', async () => {
    const response = await request(app).post('/api/summarize');
    expect(response.status).toBe(401);
  });

  it('should reject requests without authorization header on /api/embeddings', async () => {
    const response = await request(app).post('/api/embeddings');
    expect(response.status).toBe(401);
  });

  it('should reject requests without authorization header on /api/rag/generate', async () => {
    const response = await request(app).post('/api/rag/generate');
    expect(response.status).toBe(401);
  });

  it('should reject requests without authorization header on /api/generate-image', async () => {
    const response = await request(app).post('/api/generate-image');
    expect(response.status).toBe(401);
  });

  it('should reject requests with invalid token format', async () => {
    const response = await request(app)
      .post('/api/generate')
      .set('Authorization', 'InvalidFormat token');
    expect(response.status).toBe(401);
  });

  it('should reject requests with malformed Bearer token', async () => {
    const response = await request(app)
      .post('/api/generate')
      .set('Authorization', 'Bearer invalid-token');
    expect(response.status).toBe(401);
  });
});

describe('Validation Middleware', () => {
  it('should reject empty prompt on generate endpoint', async () => {
    const response = await request(app)
      .post('/api/generate')
      .send({ prompt: '' });
    expect(response.status).toBe(401);
  });
});
