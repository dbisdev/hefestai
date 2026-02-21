import { ai, geminiModel } from '../../config/index.js';
import { mapUsage, stripMarkdownCodeFences } from '../../common/index.js';
import type { SummarizeRequest, SummarizeResponse } from './summarize.schema.js';

const STYLE_INSTRUCTIONS: Record<string, string> = {
  concise: 'Provide a concise summary that captures the main points in a few sentences.',
  detailed: 'Provide a detailed summary that covers all important aspects while being comprehensive.',
  'bullet-points': 'Provide a summary in bullet-point format, with each key point as a separate item.',
};

export const summarizeFlow = ai.defineFlow(
  {
    name: 'summarize',
    inputSchema: null as any,
    outputSchema: null as any,
  },
  async (input: SummarizeRequest): Promise<SummarizeResponse> => {
    const systemPrompt = `You are an expert summarizer. ${STYLE_INSTRUCTIONS[input.style]}
The summary should be approximately ${input.maxLength} characters or less.
${input.language !== 'en' ? `Write the summary in ${input.language}.` : ''}
Focus on the most important information and maintain accuracy.`;

    const response = await ai.generate({
      model: geminiModel,
      prompt: `Please summarize the following text:\n\n${input.text}`,
      system: systemPrompt,
      config: {
        temperature: 0.3,
        maxOutputTokens: Math.ceil(input.maxLength / 3),
      },
    });

    const summary = stripMarkdownCodeFences(response.text);
    const compressionRatio = summary.length > 0
      ? Number((input.text.length / summary.length).toFixed(2))
      : 0;

    return {
      summary,
      originalLength: input.text.length,
      summaryLength: summary.length,
      compressionRatio,
      usage: mapUsage(response.usage),
    };
  }
);
