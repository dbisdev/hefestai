/**
 * Output Sanitization Utilities
 * OWASP A07: XSS Prevention through output encoding
 * Single Responsibility: Only output sanitization logic
 */

/**
 * HTML entity encoding map
 */
const HTML_ENTITIES: Record<string, string> = {
  '&': '&amp;',
  '<': '&lt;',
  '>': '&gt;',
  '"': '&quot;',
  "'": '&#x27;',
  '/': '&#x2F;',
  '`': '&#x60;',
  '=': '&#x3D;',
};

/**
 * Escapes HTML special characters to prevent XSS
 * Use this when displaying user-generated content outside of React's JSX
 * Note: React already escapes values in JSX by default
 */
export function escapeHtml(str: string): string {
  if (!str) return '';
  return str.replace(/[&<>"'`=/]/g, (char) => HTML_ENTITIES[char] || char);
}

/**
 * Sanitizes a string for use in URLs
 * Prevents injection in href/src attributes
 */
export function sanitizeUrl(url: string): string {
  if (!url) return '';
  
  // Only allow http, https, and data URLs for images
  const allowedProtocols = ['http:', 'https:', 'data:'];
  
  try {
    const parsed = new URL(url, window.location.origin);
    
    // Check for javascript: protocol and other dangerous schemes
    if (!allowedProtocols.some(p => parsed.protocol === p)) {
      console.warn('Blocked potentially dangerous URL:', url);
      return '';
    }
    
    // For data URLs, only allow images
    if (parsed.protocol === 'data:' && !url.startsWith('data:image/')) {
      console.warn('Blocked non-image data URL');
      return '';
    }
    
    return url;
  } catch {
    // If URL parsing fails, it might be a relative URL
    // Only allow if it starts with / and doesn't contain dangerous characters
    if (url.startsWith('/') && !/[<>'"`;]/.test(url)) {
      return url;
    }
    return '';
  }
}

/**
 * Sanitizes user input for logging
 * Removes sensitive data patterns and truncates long strings
 */
export function sanitizeForLogging(data: unknown, maxLength = 500): string {
  if (data === null || data === undefined) return 'null';
  
  let str = typeof data === 'string' ? data : JSON.stringify(data);
  
  // Remove potential sensitive patterns
  str = str.replace(/password['":\s]+['"][^'"]+['"]/gi, 'password:"[REDACTED]"');
  str = str.replace(/token['":\s]+['"][^'"]+['"]/gi, 'token:"[REDACTED]"');
  str = str.replace(/bearer\s+\S+/gi, 'Bearer [REDACTED]');
  
  // Truncate if too long
  if (str.length > maxLength) {
    str = str.substring(0, maxLength) + '...[truncated]';
  }
  
  return str;
}

/**
 * Strips HTML tags from a string
 * Use when you need plain text from potentially HTML content
 */
export function stripHtml(html: string): string {
  if (!html) return '';
  
  // Create a temporary element to leverage browser's HTML parser
  if (typeof document !== 'undefined') {
    const tmp = document.createElement('div');
    tmp.innerHTML = html;
    return tmp.textContent || tmp.innerText || '';
  }
  
  // Fallback for non-browser environments
  return html.replace(/<[^>]*>/g, '');
}

/**
 * Sanitizes object keys and string values recursively
 * Useful for sanitizing API responses before displaying
 */
export function sanitizeObject<T extends Record<string, unknown>>(obj: T): T {
  if (!obj || typeof obj !== 'object') return obj;
  
  const result: Record<string, unknown> = {};
  
  for (const [key, value] of Object.entries(obj)) {
    // Sanitize key
    const sanitizedKey = escapeHtml(key);
    
    // Sanitize value based on type
    if (typeof value === 'string') {
      result[sanitizedKey] = escapeHtml(value);
    } else if (Array.isArray(value)) {
      result[sanitizedKey] = value.map(item => 
        typeof item === 'string' ? escapeHtml(item) : 
        typeof item === 'object' && item !== null ? sanitizeObject(item as Record<string, unknown>) : 
        item
      );
    } else if (typeof value === 'object' && value !== null) {
      result[sanitizedKey] = sanitizeObject(value as Record<string, unknown>);
    } else {
      result[sanitizedKey] = value;
    }
  }
  
  return result as T;
}

/**
 * Creates a safe text node for manual DOM manipulation
 * Use when you need to insert text without React
 */
export function createSafeTextNode(text: string): Text {
  return document.createTextNode(text);
}
