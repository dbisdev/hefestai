import { genkit } from 'genkit';
import { googleAI } from '@genkit-ai/google-genai';

/**
 * Genkit AI instance configured with Google GenAI plugin.
 * Uses the GOOGLE_GENAI_API_KEY environment variable for authentication.
 */
export const ai = genkit({
  plugins: [
    googleAI({
      apiKey: process.env.GOOGLE_GENAI_API_KEY,
    }),
  ],
});

export { googleAI };
