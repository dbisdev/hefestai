import { describe, it, expect } from 'vitest';
import { stripMarkdownCodeFences } from '../../common/utils/strip-markdown.js';

describe('stripMarkdownCodeFences', () => {
  it('should return text unchanged if no code fences', () => {
    const input = 'Hello world';
    expect(stripMarkdownCodeFences(input)).toBe('Hello world');
  });

  it('should strip json code fence', () => {
    const input = '```json\n{"name": "test"}\n```';
    expect(stripMarkdownCodeFences(input)).toBe('{"name": "test"}');
  });

  it('should strip plain code fence', () => {
    const input = '```\ncode here\n```';
    expect(stripMarkdownCodeFences(input)).toBe('code here');
  });

  it('should strip code fence without newline after opening', () => {
    const input = '```json{"name": "test"}```';
    expect(stripMarkdownCodeFences(input)).toBe('{"name": "test"}');
  });

  it('should handle xml code fence', () => {
    const input = '```xml\n<root></root>\n```';
    expect(stripMarkdownCodeFences(input)).toBe('<root></root>');
  });

  it('should handle multiline content', () => {
    const input = '```\nline1\nline2\nline3\n```';
    expect(stripMarkdownCodeFences(input)).toBe('line1\nline2\nline3');
  });

  it('should trim whitespace', () => {
    const input = '  ```\ncode\n```  ';
    expect(stripMarkdownCodeFences(input)).toBe('code');
  });

  it('should handle text with code fence in middle (not at start)', () => {
    const input = 'Some text ```code``` more text';
    expect(stripMarkdownCodeFences(input)).toBe('Some text ```code``` more text');
  });
});
