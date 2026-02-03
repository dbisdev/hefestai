import { ai, googleAI } from './genkit.config.js';
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
  ImageGenerateRequestSchema,
  ImageGenerateResponseSchema,
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
  type ImageGenerateRequest,
  type ImageGenerateResponse,
} from './schemas.js';

/** Google GenAI model references using the new API style */
const geminiModel = googleAI.model('gemini-2.0-flash');
const geminiImageModel = googleAI.model('gemini-2.5-flash-image');
const embeddingModel = googleAI.embedder('text-embedding-004');

/**
 * Strips markdown code fences from a string.
 * Handles ```json, ```xml, ```, and other code fence variations.
 * @param text - The text potentially containing markdown code fences
 * @returns The cleaned text without code fences
 */
function stripMarkdownCodeFences(text: string): string {
  const trimmed = text.trim();
  // Check if text starts with code fence
  if (!trimmed.startsWith('```')) {
    return text;
  }
  // Remove opening code fence with optional language identifier
  let cleaned = trimmed.replace(/^```(?:\w+)?\s*\n?/, '');
  // Remove closing code fence
  cleaned = cleaned.replace(/\n?```\s*$/, '');
  return cleaned.trim();
}

/**
 * Chat flow for multi-turn conversations.
 * Supports conversation history and system context.
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
      model: geminiModel,
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
 * Simple text generation flow.
 * Generates text based on a prompt with optional system instructions.
 */
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
      ...(input.systemPrompt && {
        system: input.systemPrompt,
      }),
    });

    return {
      text: stripMarkdownCodeFences(response.text),
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
 * Text summarization flow.
 * Summarizes text with configurable style (concise, detailed, bullet-points).
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
      model: geminiModel,
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
 * Embeddings flow for RAG.
 * Generates vector embeddings for an array of text inputs.
 */
export const embeddingsFlow = ai.defineFlow(
  {
    name: 'embeddings',
    inputSchema: EmbeddingsRequestSchema,
    outputSchema: EmbeddingsResponseSchema,
  },
  async (input: EmbeddingsRequest): Promise<EmbeddingsResponse> => {
    const embeddings: number[][] = [];
    
    // Process texts individually to generate embeddings
    for (const text of input.texts) {
      const result = await ai.embed({
        embedder: embeddingModel,
        content: text,
      });
      // Genkit returns array of objects with 'embedding' property
      const embeddingVector = Array.isArray(result) 
        ? result[0].embedding 
        : (result as any).embedding || result;
      
      embeddings.push(embeddingVector);
    }

    const dimensions = embeddings[0]?.length || 768;

    return {
      embeddings,
      model: input.model || 'text-embedding-004',
      dimensions,
    };
  }
);

/**
 * RAG Generation flow.
 * Generates answers using provided context documents and a query.
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
      model: geminiModel,
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

/**
 * Image generation flow.
 * Generates a single image using Gemini's native image generation capabilities.
 */
export const imageGenerateFlow = ai.defineFlow(
  {
    name: 'imageGenerate',
    inputSchema: ImageGenerateRequestSchema,
    outputSchema: ImageGenerateResponseSchema,
  },
  async (input: ImageGenerateRequest): Promise<ImageGenerateResponse> => {
    // Style enhancement mappings
    const stylePrompts: Record<string, string> = {
      realistic: 'photorealistic, highly detailed, professional photography',
      artistic: 'artistic style, creative composition, expressive',
      anime: 'anime style, vibrant colors, Japanese animation aesthetic',
      fantasy: 'fantasy art style, magical, ethereal, detailed illustration',
      sketch: 'pencil sketch style, hand-drawn, artistic lines',
    };

    const aspectRatioHints: Record<string, string> = {
      '1:1': 'square composition',
      '16:9': 'wide landscape format, cinematic',
      '9:16': 'vertical portrait format, tall composition',
      '4:3': 'standard landscape format',
      '3:4': 'standard portrait format',
    };

    // Build enhanced prompt
    let enhancedPrompt = input.prompt;
    
    if (input.style && stylePrompts[input.style]) {
      enhancedPrompt = `${enhancedPrompt}, ${stylePrompts[input.style]}`;
    }
    
    if (input.aspectRatio && aspectRatioHints[input.aspectRatio]) {
      enhancedPrompt = `${enhancedPrompt}, ${aspectRatioHints[input.aspectRatio]}`;
    }
    
    if (input.negativePrompt) {
      enhancedPrompt = `${enhancedPrompt}. Avoid: ${input.negativePrompt}`;
    }

    const fullPrompt = `Generate an image: ${enhancedPrompt}`;

    try {
      const response = await ai.generate({
        model: geminiImageModel,
        prompt: fullPrompt,
      });

      // Extract image from response.media
      if (response.media?.contentType?.startsWith('image/')) {
        const base64Data = response.media.url.includes('base64,')
          ? response.media.url.split('base64,')[1]
          : response.media.url;
        
        return {
          image: {
            base64: base64Data,
            mimeType: response.media.contentType,
          },
          success: true,
          usedPrompt: enhancedPrompt,
        };
      }

      // Check message content for image data as fallback
      if (response.message?.content) {
        for (const part of response.message.content) {
          if ('media' in part && part.media) {
            const mediaPart = part.media as { url: string; contentType?: string };
            if (mediaPart.contentType?.startsWith('image/')) {
              const base64Data = mediaPart.url.includes('base64,')
                ? mediaPart.url.split('base64,')[1]
                : mediaPart.url;
              
              return {
                image: {
                  base64: base64Data,
                  mimeType: mediaPart.contentType,
                },
                success: true,
                usedPrompt: enhancedPrompt,
              };
            }
          }
        }
      }

      return {
        image: null,
        success: false,
        message: 'No image was generated. The model may not support image generation or the prompt was rejected.',
        usedPrompt: enhancedPrompt,
      };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      return {
        image: null,
        success: false,
        message: `Image generation failed: ${errorMessage}`,
        usedPrompt: enhancedPrompt,
      };
    }
  }
);
