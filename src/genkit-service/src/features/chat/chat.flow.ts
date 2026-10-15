import { ai, geminiModel } from '../../config/index.js';
import { mapUsage, stripMarkdownCodeFences } from '../../common/index.js';
import {
  ChatRequestSchema,
  ChatResponseSchema,
  type ChatRequest,
  type ChatResponse,
  type ChatMessage,
} from './chat.schema.js';

export const chatFlow = ai.defineFlow(
  {
    name: 'chat',
    inputSchema: ChatRequestSchema,
    outputSchema: ChatResponseSchema,
  },
  async (input: ChatRequest): Promise<ChatResponse> => {
    const messages = input.messages.map((msg: ChatMessage) => ({
      role: (msg.role === 'assistant' ? 'model' : msg.role) as 'user' | 'model' | 'system',
      content: [{ text: msg.content }],
    }));

    const lastMessage = input.messages[input.messages.length - 1];
    const previousMessages = messages.slice(0, -1);

    const response = await ai.generate({
      model: geminiModel,
      messages: previousMessages,
      prompt: lastMessage.content,
      config: {
        temperature: input.temperature,
        maxOutputTokens: input.maxTokens,
      },
      ...(input.context && { system: input.context }),
    });

    return {
      message: stripMarkdownCodeFences(response.text),
      usage: mapUsage(response.usage),
    };
  }
);
