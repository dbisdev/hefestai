/**
 * JSON Validation and Repair Utilities
 * Uses dirty-json library to parse and repair malformed JSON from AI responses
 */

import djson from 'dirty-json';

/**
 * Strip markdown code fences from text
 */
function stripMarkdownCodeFences(text: string): string {
  const trimmed = text.trim();
  if (!trimmed.startsWith('```')) {
    return text;
  }
  let cleaned = trimmed.replace(/^```(?:\w+)?\s*\n?/, '');
  cleaned = cleaned.replace(/\n?```\s*$/, '');
  return cleaned.trim();
}

/**
 * Validates and repairs JSON received from AI responses.
 * Uses dirty-json to handle malformed JSON with unescaped quotes.
 * 
 * @param jsonString - The JSON string to validate/repair
 * @returns The repaired JSON string, or null if unrepairable
 */
export function validateAndRepairJson(jsonString: string): string | null {
  if (!jsonString?.trim()) return null;

  // 1. Strip markdown code fences
  const cleaned = stripMarkdownCodeFences(jsonString);

  // 2. Try to parse as valid JSON first
  try {
    JSON.parse(cleaned);
    return cleaned;
  } catch {
    // Not valid JSON, continue to repair
  }

  // 3. Use dirty-json to parse and repair
  try {
    const parsed = djson.parse(cleaned);
    // Return as valid JSON string
    return JSON.stringify(parsed);
  } catch {
    return null;
  }
}

/**
 * Quick check if a string is valid JSON
 */
export function isValidJson(jsonString: string): boolean {
  try {
    JSON.parse(jsonString);
    return true;
  } catch {
    return false;
  }
}
