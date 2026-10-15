import { ai, embeddingModel } from '../../config/index.js';
import { EmbeddingsRequestSchema, EmbeddingsResponseSchema, type EmbeddingsRequest, type EmbeddingsResponse } from './embeddings.schema.js';

export const embeddingsFlow = ai.defineFlow(
  {
    name: 'embeddings',
    inputSchema: EmbeddingsRequestSchema,
    outputSchema: EmbeddingsResponseSchema,
  },
  async (input: EmbeddingsRequest): Promise<EmbeddingsResponse> => {
    if (!input.texts || input.texts.length === 0) {
      return {
        embeddings: [],
        model: input.model || 'gemini-embedding-001',
        dimensions: 0,
      };
    }

    const extractVector = (result: unknown): number[] =>
      Array.isArray(result)
        ? (result[0] as any).embedding
        : (result as any).embedding ?? (result as number[]);

    const embeddings = await Promise.all(
      input.texts.map((text) =>
        ai.embed({ embedder: embeddingModel, content: text }).then(extractVector)
      )
    );

    const dimensions = embeddings[0]?.length ?? 3072;

    return {
      embeddings,
      model: input.model || 'gemini-embedding-001',
      dimensions,
    };
  }
);
