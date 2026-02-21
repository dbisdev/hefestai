import { ai, geminiModel } from '../../config/index.js';
import { mapUsage, stripMarkdownCodeFences } from '../../common/index.js';
import type { TextGenerationRequest, TextGenerationResponse } from './generate.schema.js';

export const generateTextFlow = ai.defineFlow(
  {
    name: 'generateText',
    inputSchema: null as any,
    outputSchema: null as any,
  },
  async (input: TextGenerationRequest): Promise<TextGenerationResponse> => {
    const response = await ai.generate({
      model: geminiModel,
      prompt: input.prompt,
      config: {
        temperature: input.temperature,
        maxOutputTokens: input.maxTokens,
      },
      ...(input.systemPrompt && { system: input.systemPrompt }),
    });

    return {
      text: stripMarkdownCodeFences(response.text),
      usage: mapUsage(response.usage),
    };
  }
);
