import { ai, geminiImageModel } from '../../config/index.js';
import { buildEnhancedPrompt } from '../../common/index.js';
import type { ImageGenerateRequest, ImageGenerateResponse } from './generate-image.schema.js';

export const imageGenerateFlow = ai.defineFlow(
  {
    name: 'imageGenerate',
    inputSchema: null as any,
    outputSchema: null as any,
  },
  async (input: ImageGenerateRequest): Promise<ImageGenerateResponse> => {
    const enhancedPrompt = buildEnhancedPrompt(input);
    const fullPrompt = `Generate an image: ${enhancedPrompt}`;

    try {
      const response = await ai.generate({
        model: geminiImageModel,
        prompt: fullPrompt,
      });

      if (response.media?.contentType?.startsWith('image/') && response.media?.url) {
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

      if (response.message?.content) {
        for (const part of response.message.content) {
          if ('media' in part && part.media && 'url' in part.media) {
            const mediaPart = part.media as { url: string; contentType?: string };
            if (mediaPart.contentType?.startsWith('image/') && mediaPart.url) {
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
