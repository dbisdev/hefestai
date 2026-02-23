import { z } from 'zod';

export const TextGenerationRequestSchema = z.object({
  prompt: z.string().min(1).max(10000),
  systemPrompt: z.string().max(2000).optional(),
  temperature: z.number().min(0).max(2).optional().default(0.7),
  maxTokens: z.number().min(1).max(8192).optional().default(2048),
  responseFormat: z.enum(['text', 'json']).optional().default('text'),
});

export const TextGenerationResponseSchema = z.object({
  text: z.string(),
  usage: z.object({
    promptTokens: z.number(),
    completionTokens: z.number(),
    totalTokens: z.number(),
  }).optional(),
});

export type TextGenerationRequest = z.infer<typeof TextGenerationRequestSchema>;
export type TextGenerationResponse = z.infer<typeof TextGenerationResponseSchema>;
