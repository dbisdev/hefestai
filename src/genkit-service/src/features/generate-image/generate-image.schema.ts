import { z } from 'zod';

export const ImageGenerateRequestSchema = z.object({
  prompt: z.string().min(1).max(2000),
  negativePrompt: z.string().max(1000).optional(),
  aspectRatio: z.enum(['1:1', '16:9', '9:16', '4:3', '3:4']).optional().default('1:1'),
  style: z.enum(['realistic', 'artistic', 'anime', 'fantasy', 'sketch']).optional(),
});

export const ImageGenerateResponseSchema = z.object({
  image: z.object({
    base64: z.string(),
    mimeType: z.string(),
  }).nullable(),
  success: z.boolean(),
  message: z.string().optional(),
  usedPrompt: z.string().optional(),
});

export type ImageGenerateRequest = z.infer<typeof ImageGenerateRequestSchema>;
export type ImageGenerateResponse = z.infer<typeof ImageGenerateResponseSchema>;
