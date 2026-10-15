import { ai, geminiModel } from '../../config/index.js';
import { mapUsage, stripMarkdownCodeFences, validateAndRepairJson } from '../../common/index.js';
import {
  TextGenerationRequestSchema,
  TextGenerationResponseSchema,
  type TextGenerationRequest,
  type TextGenerationResponse,
} from './generate.schema.js';

export const generateTextFlow = ai.defineFlow(
  {
    name: 'generateText',
    inputSchema: TextGenerationRequestSchema,
    outputSchema: TextGenerationResponseSchema,
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

    const rawText = response.text;
    let cleanedText: string;

    // Validate and repair JSON if response format is JSON
    if (input.responseFormat === 'json') {
      const repaired = validateAndRepairJson(rawText);
      cleanedText = repaired ?? stripMarkdownCodeFences(rawText);
    } else {
      cleanedText = stripMarkdownCodeFences(rawText);
    }

    return {
      text: cleanedText,
      usage: mapUsage(response.usage),
    };
  }
);
