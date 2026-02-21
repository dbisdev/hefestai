export interface TokenUsage {
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
}

export function createTokenUsage(inputTokens?: number, outputTokens?: number): TokenUsage {
  return {
    promptTokens: inputTokens ?? 0,
    completionTokens: outputTokens ?? 0,
    totalTokens: (inputTokens ?? 0) + (outputTokens ?? 0),
  };
}
