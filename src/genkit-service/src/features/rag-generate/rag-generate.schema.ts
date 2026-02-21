import { z } from 'zod';

export const RagGenerateRequestSchema = z.object({
  query: z.string().min(1).max(8000),
  context: z.array(z.string()).min(1).max(10),
  systemPrompt: z.string().max(4000).optional(),
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

export type RagGenerateRequest = z.infer<typeof RagGenerateRequestSchema>;
export type RagGenerateResponse = z.infer<typeof RagGenerateResponseSchema>;
