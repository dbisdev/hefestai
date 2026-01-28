import { z } from 'zod';

// JWT Token payload schema
export const ServiceTokenPayloadSchema = z.object({
  sub: z.string(), // Subject - service identifier
  iss: z.string(), // Issuer
  aud: z.string(), // Audience
  scope: z.string(), // Space-separated scopes
  iat: z.number(), // Issued at
  exp: z.number(), // Expiration
});

export type ServiceTokenPayload = z.infer<typeof ServiceTokenPayloadSchema>;

// Available scopes
export const Scopes = {
  GENKIT_EXECUTE: 'genkit.execute',
  GENKIT_ADMIN: 'genkit.admin',
} as const;

export type Scope = (typeof Scopes)[keyof typeof Scopes];
