import { ai } from './genkit.config.js';
import { gemini15Flash, textEmbedding004 } from '@genkit-ai/googleai';
import {
  ChatRequestSchema,
  ChatResponseSchema,
  TextGenerationRequestSchema,
  TextGenerationResponseSchema,
  SummarizeRequestSchema,
  SummarizeResponseSchema,
  EmbeddingsRequestSchema,
  EmbeddingsResponseSchema,
  RagGenerateRequestSchema,
  RagGenerateResponseSchema,
  type ChatRequest,
  type ChatResponse,
  type TextGenerationRequest,
  type TextGenerationResponse,
  type SummarizeRequest,
  type SummarizeResponse,
  type EmbeddingsRequest,
  type EmbeddingsResponse,
  type RagGenerateRequest,
  type RagGenerateResponse,
} from './schemas.js';

/**
 * Chat flow for multi-turn conversations
 */
export const chatFlow = ai.defineFlow(
  {
    name: 'chat',
    inputSchema: ChatRequestSchema,
    outputSchema: ChatResponseSchema,
  },
  async (input: ChatRequest): Promise<ChatResponse> => {
    // Convert message roles to Genkit format (assistant -> model)
    const messages = input.messages.map((msg) => ({
      role: (msg.role === 'assistant' ? 'model' : msg.role) as 'user' | 'model' | 'system',
      content: [{ text: msg.content }],
    }));

    // Build the prompt from the last user message
    const lastMessage = input.messages[input.messages.length - 1];
    const previousMessages = messages.slice(0, -1);

    const response = await ai.generate({
      model: gemini15Flash,
      messages: previousMessages,
      prompt: lastMessage.content,
      config: {
        temperature: input.temperature,
        maxOutputTokens: input.maxTokens,
      },
      ...(input.context && {
        system: input.context,
      }),
    });

    return {
      message: response.text,
      usage: response.usage
        ? {
            promptTokens: response.usage.inputTokens || 0,
            completionTokens: response.usage.outputTokens || 0,
            totalTokens: (response.usage.inputTokens || 0) + (response.usage.outputTokens || 0),
          }
        : undefined,
    };
  }
);

/**
 * Simple text generation flow
 */
export const generateTextFlow = ai.defineFlow(
  {
    name: 'generateText',
    inputSchema: TextGenerationRequestSchema,
    outputSchema: TextGenerationResponseSchema,
  },
  async (input: TextGenerationRequest): Promise<TextGenerationResponse> => {
    const response = await ai.generate({
      model: gemini15Flash,
      prompt: input.prompt,
      config: {
        temperature: input.temperature,
        maxOutputTokens: input.maxTokens,
      },
      ...(input.systemPrompt && {
        system: input.systemPrompt,
      }),
    });

    return {
      text: response.text,
      usage: response.usage
        ? {
            promptTokens: response.usage.inputTokens || 0,
            completionTokens: response.usage.outputTokens || 0,
            totalTokens: (response.usage.inputTokens || 0) + (response.usage.outputTokens || 0),
          }
        : undefined,
    };
  }
);

/**
 * Text summarization flow
 */
export const summarizeFlow = ai.defineFlow(
  {
    name: 'summarize',
    inputSchema: SummarizeRequestSchema,
    outputSchema: SummarizeResponseSchema,
  },
  async (input: SummarizeRequest): Promise<SummarizeResponse> => {
    const styleInstructions = {
      concise: 'Provide a concise summary that captures the main points in a few sentences.',
      detailed: 'Provide a detailed summary that covers all important aspects while being comprehensive.',
      'bullet-points': 'Provide a summary in bullet-point format, with each key point as a separate item.',
    };

    const systemPrompt = `You are an expert summarizer. ${styleInstructions[input.style]}
The summary should be approximately ${input.maxLength} characters or less.
${input.language !== 'en' ? `Write the summary in ${input.language}.` : ''}
Focus on the most important information and maintain accuracy.`;

    const response = await ai.generate({
      model: gemini15Flash,
      prompt: `Please summarize the following text:\n\n${input.text}`,
      system: systemPrompt,
      config: {
        temperature: 0.3, // Lower temperature for more consistent summaries
        maxOutputTokens: Math.ceil(input.maxLength / 3), // Rough estimate for tokens
      },
    });

    const summary = response.text;

    return {
      summary,
      originalLength: input.text.length,
      summaryLength: summary.length,
      compressionRatio: Number((input.text.length / summary.length).toFixed(2)),
      usage: response.usage
        ? {
            promptTokens: response.usage.inputTokens || 0,
            completionTokens: response.usage.outputTokens || 0,
            totalTokens: (response.usage.inputTokens || 0) + (response.usage.outputTokens || 0),
          }
        : undefined,
    };
  }
);

/**
 * Embeddings flow for RAG - generates vector embeddings for text
 */
export const embeddingsFlow = ai.defineFlow(
  {
    name: 'embeddings',
    inputSchema: EmbeddingsRequestSchema,
    outputSchema: EmbeddingsResponseSchema,
  },
  async (input: EmbeddingsRequest): Promise<EmbeddingsResponse> => {
    const embeddings: number[][] = [];
    
    // Process texts in batches to avoid rate limits
    for (const text of input.texts) {
      const result = await ai.embed({
        embedder: textEmbedding004,
        content: text,
      });
      // Genkit 1.0 returns array of objects with 'embedding' property
      // For single content, we get an array with one element
      const embeddingVector = Array.isArray(result) 
        ? result[0].embedding 
        : (result as any).embedding || result;
      embeddings.push(embeddingVector);
    }

    return {
      embeddings,
      model: input.model || 'text-embedding-004',
      dimensions: embeddings[0]?.length || 768,
    };
  }
);

/**
 * RAG Generation flow - generates answers using provided context
 */
export const ragGenerateFlow = ai.defineFlow(
  {
    name: 'ragGenerate',
    inputSchema: RagGenerateRequestSchema,
    outputSchema: RagGenerateResponseSchema,
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
      model: gemini15Flash,
      prompt,
      system: systemPrompt,
      config: {
        temperature: input.temperature,
        maxOutputTokens: input.maxTokens,
      },
    });

    return {
      answer: response.text,
      usage: response.usage
        ? {
            promptTokens: response.usage.inputTokens || 0,
            completionTokens: response.usage.outputTokens || 0,
            totalTokens: (response.usage.inputTokens || 0) + (response.usage.outputTokens || 0),
          }
        : undefined,
    };
  }
);
