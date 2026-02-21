import { ai, embeddingModel } from '../../config/index.js';
import type { EmbeddingsRequest, EmbeddingsResponse } from './embeddings.schema.js';

export const embeddingsFlow = ai.defineFlow(
  {
    name: 'embeddings',
    inputSchema: null as any,
    outputSchema: null as any,
  },
  async (input: EmbeddingsRequest): Promise<EmbeddingsResponse> => {
    if (!input.texts || input.texts.length === 0) {
      return {
        embeddings: [],
        model: input.model || 'gemini-embedding-001',
        dimensions: 0,
      };
    }

    const embeddings: number[][] = [];

    for (const text of input.texts) {
      const result = await ai.embed({
        embedder: embeddingModel,
        content: text,
      });
      const embeddingVector = Array.isArray(result)
        ? result[0].embedding
        : (result as any).embedding || result;
      embeddings.push(embeddingVector);
    }

    const dimensions = embeddings[0]?.length || 3072;

    return {
      embeddings,
      model: input.model || 'gemini-embedding-001',
      dimensions,
    };
  }
);
