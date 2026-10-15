/**
 * Utility for extracting image data from a Genkit `generate` response.
 *
 * The Genkit SDK can surface image data in two locations depending on the model
 * version and response shape:
 *  1. `response.media` — top-level media shortcut.
 *  2. `response.message.content[].media` — individual content parts.
 *
 * Both locations are probed in priority order; `null` is returned when no image
 * is found so the caller can return an appropriate failure response.
 */

/** Minimal subset of the Genkit `GenerateResponse` that contains image data. */
export interface GenkitImageResponse {
  media?: { contentType?: string; url: string } | null;
  message?: { content?: unknown[] } | null;
}

/** Extracted image data, or `null` when no image was found in the response. */
export interface ExtractedImage {
  base64: string;
  mimeType: string;
}

/**
 * Extracts image data from a Genkit generate response.
 *
 * @param response - The raw response object returned by `ai.generate(...)`.
 * @returns The extracted image data, or `null` if no image is present.
 */
export function extractImageFromResponse(response: GenkitImageResponse): ExtractedImage | null {
  // 1. Top-level media shortcut (most models)
  if (response.media?.contentType?.startsWith('image/') && response.media?.url) {
    const base64 = response.media.url.includes('base64,')
      ? response.media.url.split('base64,')[1]
      : response.media.url;
    return { base64, mimeType: response.media.contentType };
  }

  // 2. Per-part content array (some multi-modal models)
  if (response.message?.content) {
    for (const part of response.message.content) {
      if (typeof part === 'object' && part !== null && 'media' in part) {
        const mediaPart = (part as { media: { url: string; contentType?: string } }).media;
        if (mediaPart?.contentType?.startsWith('image/') && mediaPart?.url) {
          const base64 = mediaPart.url.includes('base64,')
            ? mediaPart.url.split('base64,')[1]
            : mediaPart.url;
          return { base64, mimeType: mediaPart.contentType };
        }
      }
    }
  }

  return null;
}
