import { ai, geminiImageModel } from '../../config/index.js';
import { buildEnhancedPrompt, extractImageFromResponse } from '../../common/index.js';
import { logger } from '../../logger.js';
import { ImageGenerateRequestSchema, ImageGenerateResponseSchema, type ImageGenerateRequest, type ImageGenerateResponse } from './generate-image.schema.js';

export const imageGenerateFlow = ai.defineFlow(
  {
    name: 'imageGenerate',
    inputSchema: ImageGenerateRequestSchema,
    outputSchema: ImageGenerateResponseSchema,
  },
  async (input: ImageGenerateRequest): Promise<ImageGenerateResponse> => {
    const enhancedPrompt = buildEnhancedPrompt(input);
    const fullPrompt = `Generate an image: ${enhancedPrompt}`;
    logger.debug({ fullPrompt }, 'Full prompt for image generation');
    try {
      const response = await ai.generate({
        model: geminiImageModel,
        prompt: fullPrompt,
      });

      const extracted = extractImageFromResponse(response);
      if (extracted) {
        return {
          image: {
            base64: extracted.base64,
            mimeType: extracted.mimeType,
          },
          success: true,
          usedPrompt: enhancedPrompt,
        };
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
