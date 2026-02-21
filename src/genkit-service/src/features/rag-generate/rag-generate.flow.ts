import { ai, geminiModel } from '../../config/index.js';
import { mapUsage, stripMarkdownCodeFences } from '../../common/index.js';
import type { RagGenerateRequest, RagGenerateResponse } from './rag-generate.schema.js';

export const ragGenerateFlow = ai.defineFlow(
  {
    name: 'ragGenerate',
    inputSchema: null as any,
    outputSchema: null as any,
  },
  async (input: RagGenerateRequest): Promise<RagGenerateResponse> => {
    const contextText = input.context
      .map((ctx, i) => `[Document ${i + 1}]:\n${ctx}`)
      .join('\n\n');

    const systemPrompt = input.systemPrompt ||
      `You are a helpful assistant that answers questions based on the provided context.
Only use information from the context to answer. If the context doesn't contain enough information, say so.
Be concise and accurate.`;

    const prompt = `Context:\n${contextText}\n\nQuestion: ${input.query}\n\nAnswer:`;

    const response = await ai.generate({
      model: geminiModel,
      prompt,
      system: systemPrompt,
      config: {
        temperature: input.temperature,
        maxOutputTokens: input.maxTokens,
      },
    });

    return {
      answer: stripMarkdownCodeFences(response.text),
      usage: mapUsage(response.usage),
    };
  }
);
