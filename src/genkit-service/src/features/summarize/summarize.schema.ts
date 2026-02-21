import { z } from 'zod';

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

export type SummarizeRequest = z.infer<typeof SummarizeRequestSchema>;
export type SummarizeResponse = z.infer<typeof SummarizeResponseSchema>;
