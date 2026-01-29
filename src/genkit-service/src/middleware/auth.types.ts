import { z } from 'zod';

/**
 * JWT Token payload schema for service-to-service authentication.
 * 
 * Note: iat/exp can be numbers (standard JWT) or strings (some JWT libraries
 * serialize numeric claims as strings). We coerce strings to numbers for compatibility.
 */
export const ServiceTokenPayloadSchema = z.object({
  sub: z.string(), // Subject - service identifier
  iss: z.string(), // Issuer
  aud: z.string(), // Audience
  scope: z.string(), // Space-separated scopes
  iat: z.union([z.number(), z.string().transform((v) => parseInt(v, 10))]), // Issued at
  exp: z.union([z.number(), z.string().transform((v) => parseInt(v, 10))]), // Expiration
});

export type ServiceTokenPayload = z.infer<typeof ServiceTokenPayloadSchema>;

// Available scopes
export const Scopes = {
  GENKIT_EXECUTE: 'genkit.execute',
  GENKIT_ADMIN: 'genkit.admin',
} as const;

export type Scope = (typeof Scopes)[keyof typeof Scopes];
