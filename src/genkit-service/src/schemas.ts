import { z } from 'zod';

// Common schemas for AI requests and responses

export const ChatMessageSchema = z.object({
  role: z.enum(['user', 'assistant', 'system']),
  content: z.string(),
});

export const ChatRequestSchema = z.object({
  messages: z.array(ChatMessageSchema),
  context: z.string().optional(),
  temperature: z.number().min(0).max(2).optional().default(0.7),
  maxTokens: z.number().min(1).max(8192).optional().default(2048),
});

export const ChatResponseSchema = z.object({
  message: z.string(),
  usage: z.object({
    promptTokens: z.number(),
    completionTokens: z.number(),
    totalTokens: z.number(),
  }).optional(),
});

export const TextGenerationRequestSchema = z.object({
  prompt: z.string().min(1).max(10000),
  systemPrompt: z.string().max(2000).optional(),
  temperature: z.number().min(0).max(2).optional().default(0.7),
  maxTokens: z.number().min(1).max(8192).optional().default(2048),
});

export const TextGenerationResponseSchema = z.object({
  text: z.string(),
  usage: z.object({
    promptTokens: z.number(),
    completionTokens: z.number(),
    totalTokens: z.number(),
  }).optional(),
});

// Summarization schemas
export const SummarizeRequestSchema = z.object({
  text: z.string().min(10).max(50000),
  style: z.enum(['concise', 'detailed', 'bullet-points']).optional().default('concise'),
  maxLength: z.number().min(50).max(2000).optional().default(500),
  language: z.string().optional().default('en'),
});

export const SummarizeResponseSchema = z.object({
  summary: z.string(),
  originalLength: z.number(),
  summaryLength: z.number(),
  compressionRatio: z.number(),
  usage: z.object({
    promptTokens: z.number(),
    completionTokens: z.number(),
    totalTokens: z.number(),
  }).optional(),
});

// =============================================================================
// Embeddings schemas (for RAG)
// =============================================================================
export const EmbeddingsRequestSchema = z.object({
  texts: z.array(z.string().min(1).max(8000)).min(1).max(100),
  model: z.string().optional().default('text-embedding-004'),
});

export const EmbeddingsResponseSchema = z.object({
  embeddings: z.array(z.array(z.number())),
  model: z.string(),
  dimensions: z.number(),
});

// =============================================================================
// RAG Generation schemas
// =============================================================================
export const RagGenerateRequestSchema = z.object({
  query: z.string().min(1).max(2000),
  context: z.array(z.string()).min(1).max(10),
  systemPrompt: z.string().max(2000).optional(),
  temperature: z.number().min(0).max(2).optional().default(0.3),
  maxTokens: z.number().min(1).max(8192).optional().default(2048),
});

export const RagGenerateResponseSchema = z.object({
  answer: z.string(),
  usage: z.object({
    promptTokens: z.number(),
    completionTokens: z.number(),
    totalTokens: z.number(),
  }).optional(),
});

// Type exports
export type ChatMessage = z.infer<typeof ChatMessageSchema>;
export type ChatRequest = z.infer<typeof ChatRequestSchema>;
export type ChatResponse = z.infer<typeof ChatResponseSchema>;
export type TextGenerationRequest = z.infer<typeof TextGenerationRequestSchema>;
export type TextGenerationResponse = z.infer<typeof TextGenerationResponseSchema>;
export type SummarizeRequest = z.infer<typeof SummarizeRequestSchema>;
export type SummarizeResponse = z.infer<typeof SummarizeResponseSchema>;
export type EmbeddingsRequest = z.infer<typeof EmbeddingsRequestSchema>;
export type EmbeddingsResponse = z.infer<typeof EmbeddingsResponseSchema>;
export type RagGenerateRequest = z.infer<typeof RagGenerateRequestSchema>;
export type RagGenerateResponse = z.infer<typeof RagGenerateResponseSchema>;

// =============================================================================
// Image Generation schemas
// =============================================================================
export const ImageGenerateRequestSchema = z.object({
  /** The prompt describing the image to generate */
  prompt: z.string().min(1).max(2000),
  /** Optional negative prompt to specify what to avoid */
  negativePrompt: z.string().max(1000).optional(),
  /** Aspect ratio of the generated image */
  aspectRatio: z.enum(['1:1', '16:9', '9:16', '4:3', '3:4']).optional().default('1:1'),
  /** Style preset for the image */
  style: z.enum(['realistic', 'artistic', 'anime', 'fantasy', 'sketch']).optional(),
});

export const ImageGenerateResponseSchema = z.object({
  /** Generated image as base64 string */
  image: z.object({
    base64: z.string(),
    mimeType: z.string(),
  }).nullable(),
  /** Whether the generation was successful */
  success: z.boolean(),
  /** Optional message (error details or additional info) */
  message: z.string().optional(),
  /** The prompt that was used (may be enhanced) */
  usedPrompt: z.string().optional(),
});

export type ImageGenerateRequest = z.infer<typeof ImageGenerateRequestSchema>;
export type ImageGenerateResponse = z.infer<typeof ImageGenerateResponseSchema>;
