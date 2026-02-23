/**
 * Tests for JSON validation and repair utilities
 */

import { describe, it, expect } from 'vitest';
import { validateAndRepairJson, isValidJson } from '../../common/utils/validate-json.js';

describe('validateAndRepairJson', () => {
  describe('valid JSON', () => {
    it('returns valid JSON unchanged', () => {
      const input = '{"name": "test", "value": 123}';
      const result = validateAndRepairJson(input);
      expect(result).toBe(input);
    });

    it('handles nested objects', () => {
      const input = '{"outer": {"inner": {"deep": true}}}';
      expect(validateAndRepairJson(input)).toBe(input);
    });

    it('handles arrays', () => {
      const input = '{"items": [1, 2, 3]}';
      expect(validateAndRepairJson(input)).toBe(input);
    });

    it('handles empty strings and null values', () => {
      const input = '{"empty": "", "null": null}';
      expect(validateAndRepairJson(input)).toBe(input);
    });
  });

  describe('markdown code fences', () => {
    it('strips ```json fences', () => {
      const input = '```json\n{"name": "test"}\n```';
      expect(validateAndRepairJson(input)).toBe('{"name": "test"}');
    });

    it('strips ``` fences without language', () => {
      const input = '```\n{"name": "test"}\n```';
      expect(validateAndRepairJson(input)).toBe('{"name": "test"}');
    });

    it('strips fences with extra whitespace', () => {
      const input = '```json  \n  {"name": "test"}  \n```';
      expect(validateAndRepairJson(input)).toBe('{"name": "test"}');
    });
  });

  describe('unescaped quotes', () => {
    it('repairs unescaped quotes inside string values', () => {
      const input = '{"talent": "Use "COMMAND" skill"}';
      const result = validateAndRepairJson(input);
      expect(result).not.toBeNull();
      expect(() => JSON.parse(result!)).not.toThrow();
      const parsed = JSON.parse(result!);
      expect(parsed.talent).toBe('Use "COMMAND" skill');
    });

    it('repairs multiple unescaped quotes', () => {
      const input = '{"desc": "A "fearless" warrior with "strong" will"}';
      const result = validateAndRepairJson(input);
      expect(result).not.toBeNull();
      expect(() => JSON.parse(result!)).not.toThrow();
    });

    it('handles mixed escaped and unescaped quotes', () => {
      const input = '{"text": "This is \\"good" but this is bad"}';
      const result = validateAndRepairJson(input);
      expect(result).not.toBeNull();
      expect(() => JSON.parse(result!)).not.toThrow();
    });
  });

  describe('edge cases', () => {
    it('returns null for empty string', () => {
      expect(validateAndRepairJson('')).toBeNull();
    });

    it('returns null for whitespace only', () => {
      expect(validateAndRepairJson('   ')).toBeNull();
    });

    it('returns null for null input', () => {
      expect(validateAndRepairJson(null as any)).toBeNull();
    });

    it('returns null for unrepairable JSON', () => {
      expect(validateAndRepairJson('{this is not json at all')).toBeNull();
    });
  });

  describe('real-world AI output scenarios', () => {
    it('handles AI output with quotes in description', () => {
      const input = '{"name":"Angela Harris","description":"She\'s known as "The Hunter" in the frontier."}';
      const result = validateAndRepairJson(input);
      expect(result).not.toBeNull();
      expect(() => JSON.parse(result!)).not.toThrow();
    });

    it('handles nested quotes in talent field', () => {
      const input = '{"talent":"Authority. You can use your "COMMAND" skill instead of "MANIPULATION" to get someone to bend to your will."}';
      const result = validateAndRepairJson(input);
      expect(result).not.toBeNull();
      expect(() => JSON.parse(result!)).not.toThrow();
    });

    it('handles complex nested object with quotes', () => {
      const input = '{"name": "Character "The Brave"", "gear": {"weapon": "Sword of "Justice""}}';
      const result = validateAndRepairJson(input);
      expect(result).not.toBeNull();
      expect(() => JSON.parse(result!)).not.toThrow();
    });
  });
});

describe('isValidJson', () => {
  it('returns true for valid JSON', () => {
    expect(isValidJson('{"name": "test"}')).toBe(true);
  });

  it('returns false for invalid JSON', () => {
    expect(isValidJson('{invalid}')).toBe(false);
  });

  it('returns true for valid JSON arrays', () => {
    expect(isValidJson('[1, 2, 3]')).toBe(true);
  });

  it('returns false for truncated JSON', () => {
    expect(isValidJson('{"name": "test"')).toBe(false);
  });
});
