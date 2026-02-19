/**
 * Security Utilities Tests
 * Tests for security-related utility functions:
 * - Token generation
 * - URL validation
 * - Data masking
 */
import { describe, it, expect } from 'vitest';
import {
  generateSecureToken,
  isSecureContext,
  isSafeRedirectUrl,
  maskSensitiveData,
  generateCSPNonce,
} from '@core/utils/security';

describe('Security Utilities', () => {
  describe('generateSecureToken', () => {
    it('generates a token of default length', () => {
      const token = generateSecureToken();
      expect(token).toHaveLength(64);
    });

    it('generates a token of custom length', () => {
      const token = generateSecureToken(32);
      expect(token).toHaveLength(64); // hex encoding doubles the length
    });

    it('generates unique tokens', () => {
      const token1 = generateSecureToken();
      const token2 = generateSecureToken();
      expect(token1).not.toBe(token2);
    });

    it('generates tokens with only hex characters', () => {
      const token = generateSecureToken();
      expect(token).toMatch(/^[0-9a-f]+$/);
    });
  });

  describe('isSecureContext', () => {
    it('returns true for HTTPS', () => {
      Object.defineProperty(window, 'location', {
        value: { protocol: 'https:' },
        writable: true,
      });
      
      expect(isSecureContext()).toBe(true);
    });

    it('returns true for localhost', () => {
      Object.defineProperty(window, 'location', {
        value: { protocol: 'http:', hostname: 'localhost' },
        writable: true,
      });
      
      expect(isSecureContext()).toBe(true);
    });
  });

  describe('isSafeRedirectUrl', () => {
    it('returns true for relative URLs starting with /', () => {
      expect(isSafeRedirectUrl('/dashboard')).toBe(true);
    });

    it('returns false for protocol-relative URLs', () => {
      expect(isSafeRedirectUrl('//evil.com')).toBe(false);
    });
  });

  describe('maskSensitiveData', () => {
    it('masks short strings with asterisks', () => {
      const masked = maskSensitiveData('abc', 2);
      expect(masked).toBe('***');
    });

    it('handles empty strings', () => {
      const masked = maskSensitiveData('');
      expect(masked).toBe('');
    });

    it('handles null/undefined', () => {
      expect(maskSensitiveData(null as unknown as string)).toBe('');
      expect(maskSensitiveData(undefined as unknown as string)).toBe('');
    });
  });

  describe('generateCSPNonce', () => {
    it('generates a nonce of appropriate length', () => {
      const nonce = generateCSPNonce();
      expect(nonce).toHaveLength(32);
    });

    it('generates unique nonces', () => {
      const nonce1 = generateCSPNonce();
      const nonce2 = generateCSPNonce();
      expect(nonce1).not.toBe(nonce2);
    });
  });
});
