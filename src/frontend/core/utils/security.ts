/**
 * Security Utilities
 * OWASP compliant security helpers
 * Single Responsibility: Security-related utility functions
 */

/**
 * Generates a cryptographically secure random string
 * Useful for CSRF tokens, nonces, etc.
 */
export function generateSecureToken(length = 32): string {
  const array = new Uint8Array(length);
  crypto.getRandomValues(array);
  return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
}

/**
 * Rate limiting tracker for client-side rate limiting awareness
 */
class RateLimiter {
  private requests: number[] = [];
  private readonly windowMs: number;
  private readonly maxRequests: number;

  constructor(windowMs = 60000, maxRequests = 100) {
    this.windowMs = windowMs;
    this.maxRequests = maxRequests;
  }

  canMakeRequest(): boolean {
    const now = Date.now();
    this.requests = this.requests.filter(time => now - time < this.windowMs);
    return this.requests.length < this.maxRequests;
  }

  recordRequest(): void {
    this.requests.push(Date.now());
  }

  getRemainingRequests(): number {
    const now = Date.now();
    this.requests = this.requests.filter(time => now - time < this.windowMs);
    return Math.max(0, this.maxRequests - this.requests.length);
  }

  getResetTime(): number {
    if (this.requests.length === 0) return 0;
    const oldestRequest = Math.min(...this.requests);
    return Math.max(0, oldestRequest + this.windowMs - Date.now());
  }
}

// Singleton rate limiter instance
export const rateLimiter = new RateLimiter();

/**
 * Login attempt tracker for brute force protection awareness
 */
class LoginAttemptTracker {
  private attempts: Map<string, { count: number; lockedUntil: number }> = new Map();
  private readonly maxAttempts: number;
  private readonly lockoutDurationMs: number;

  constructor(maxAttempts = 5, lockoutDurationMs = 15 * 60 * 1000) {
    this.maxAttempts = maxAttempts;
    this.lockoutDurationMs = lockoutDurationMs;
  }

  recordFailedAttempt(identifier: string): void {
    const existing = this.attempts.get(identifier) || { count: 0, lockedUntil: 0 };
    existing.count++;
    
    if (existing.count >= this.maxAttempts) {
      existing.lockedUntil = Date.now() + this.lockoutDurationMs;
    }
    
    this.attempts.set(identifier, existing);
  }

  isLocked(identifier: string): boolean {
    const record = this.attempts.get(identifier);
    if (!record) return false;
    
    if (record.lockedUntil && Date.now() < record.lockedUntil) {
      return true;
    }
    
    // Reset if lockout expired
    if (record.lockedUntil && Date.now() >= record.lockedUntil) {
      this.attempts.delete(identifier);
    }
    
    return false;
  }

  getLockoutRemainingMs(identifier: string): number {
    const record = this.attempts.get(identifier);
    if (!record || !record.lockedUntil) return 0;
    return Math.max(0, record.lockedUntil - Date.now());
  }

  clearAttempts(identifier: string): void {
    this.attempts.delete(identifier);
  }
}

// Singleton login tracker instance
export const loginAttemptTracker = new LoginAttemptTracker();

/**
 * Checks if the current page is being served over HTTPS
 * OWASP A02: Cryptographic Failures - ensure secure transport
 */
export function isSecureContext(): boolean {
  return window.location.protocol === 'https:' || window.location.hostname === 'localhost';
}

/**
 * Validates that a redirect URL is safe (same origin)
 * Prevents open redirect vulnerabilities
 */
export function isSafeRedirectUrl(url: string): boolean {
  try {
    const parsed = new URL(url, window.location.origin);
    return parsed.origin === window.location.origin;
  } catch {
    // If it's a relative URL, it's safe
    return url.startsWith('/') && !url.startsWith('//');
  }
}

/**
 * Masks sensitive data for display (e.g., email, tokens)
 */
export function maskSensitiveData(data: string, visibleChars = 4): string {
  if (!data || data.length <= visibleChars * 2) {
    return '*'.repeat(data?.length || 0);
  }
  
  const start = data.substring(0, visibleChars);
  const end = data.substring(data.length - visibleChars);
  const masked = '*'.repeat(Math.min(data.length - visibleChars * 2, 10));
  
  return `${start}${masked}${end}`;
}

/**
 * Content Security Policy nonce generator
 * For inline scripts if needed
 */
export function generateCSPNonce(): string {
  return generateSecureToken(16);
}
