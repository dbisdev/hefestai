import { genkit } from 'genkit';
import { googleAI } from '@genkit-ai/google-genai';

export const ai = genkit({
  plugins: [
    googleAI({
      apiKey: process.env.GOOGLE_GENAI_API_KEY,
    }),
  ],
});

export { googleAI };

export const geminiModel = googleAI.model('gemini-2.0-flash');
export const geminiImageModel = googleAI.model('gemini-2.5-flash-image');
export const embeddingModel = googleAI.embedder('gemini-embedding-001');
