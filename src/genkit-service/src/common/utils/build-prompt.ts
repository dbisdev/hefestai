import type { ImageGenerateRequest } from '../../features/generate-image/generate-image.schema.js';

const STYLE_PROMPTS: Record<string, string> = {
  realistic: 'photorealistic, highly detailed, professional photography',
  artistic: 'artistic style, creative composition, expressive',
  anime: 'anime style, vibrant colors, Japanese animation aesthetic',
  fantasy: 'fantasy art style, magical, ethereal, detailed illustration',
  sketch: 'pencil sketch style, hand-drawn, artistic lines',
};

const ASPECT_RATIO_HINTS: Record<string, string> = {
  '1:1': 'square composition',
  '16:9': 'wide landscape format, cinematic',
  '9:16': 'vertical portrait format, tall composition',
  '4:3': 'standard landscape format',
  '3:4': 'standard portrait format',
};

export function buildEnhancedPrompt(input: ImageGenerateRequest): string {
  let enhancedPrompt = input.prompt;

  if (input.style && STYLE_PROMPTS[input.style]) {
    enhancedPrompt = `${enhancedPrompt}, ${STYLE_PROMPTS[input.style]}`;
  }

  if (input.aspectRatio && ASPECT_RATIO_HINTS[input.aspectRatio]) {
    enhancedPrompt = `${enhancedPrompt}, ${ASPECT_RATIO_HINTS[input.aspectRatio]}`;
  }

  if (input.negativePrompt) {
    enhancedPrompt = `${enhancedPrompt}. Avoid: ${input.negativePrompt}`;
  }

  return enhancedPrompt;
}

export const STYLE_PROMPTS_EXPORT = STYLE_PROMPTS;
export const ASPECT_RATIO_HINTS_EXPORT = ASPECT_RATIO_HINTS;
