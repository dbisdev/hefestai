import { z } from 'zod';

export const EmbeddingsRequestSchema = z.object({
  texts: z.array(z.string().min(1).max(8000)).min(1).max(100),
  model: z.string().optional().default('gemini-embedding-001'),
});

export const EmbeddingsResponseSchema = z.object({
  embeddings: z.array(z.array(z.number())),
  model: z.string(),
  dimensions: z.number(),
});

export type EmbeddingsRequest = z.infer<typeof EmbeddingsRequestSchema>;
export type EmbeddingsResponse = z.infer<typeof EmbeddingsResponseSchema>;
