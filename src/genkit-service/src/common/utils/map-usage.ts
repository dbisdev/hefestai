import { createTokenUsage, type TokenUsage } from '../types/usage.js';

interface GenkitUsage {
  inputTokens?: number;
  outputTokens?: number;
}

export function mapUsage(usage?: GenkitUsage): TokenUsage | undefined {
  if (!usage) return undefined;
  return createTokenUsage(usage.inputTokens, usage.outputTokens);
}
