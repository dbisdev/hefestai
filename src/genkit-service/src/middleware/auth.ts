import type { Request, Response, NextFunction } from 'express';
import jwt from 'jsonwebtoken';
import { ServiceTokenPayloadSchema, type ServiceTokenPayload, type Scope } from './auth.types.js';
import { logger } from '../logger.js';

// Extend Express Request to include service info
declare global {
  namespace Express {
    interface Request {
      service?: {
        id: string;
        scopes: string[];
      };
    }
  }
}

const JWT_SECRET = process.env.JWT_SECRET;
const JWT_ISSUER = process.env.JWT_ISSUER || 'Loremaster.Api';
const JWT_AUDIENCE = process.env.JWT_AUDIENCE || 'Loremaster.Genkit';

if (!JWT_SECRET) {
  throw new Error('JWT_SECRET environment variable is required');
}

/**
 * Middleware to validate service JWT tokens
 */
export function authenticateService(req: Request, res: Response, next: NextFunction): void {
  const authHeader = req.headers.authorization;

  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    logger.warn({ path: req.path }, 'Missing or invalid authorization header');
    res.status(401).json({
      error: 'Unauthorized',
      message: 'Missing or invalid authorization header',
    });
    return;
  }

  const token = authHeader.substring(7);

  try {
    const decoded = jwt.verify(token, JWT_SECRET!, {
      issuer: JWT_ISSUER,
      audience: JWT_AUDIENCE,
      algorithms: ['HS256'],
    }) as Record<string, unknown>;

    // Validate payload structure
    const payload = ServiceTokenPayloadSchema.parse(decoded);

    // Attach service info to request
    req.service = {
      id: payload.sub,
      scopes: payload.scope.split(' ').filter(Boolean),
    };

    logger.debug({ serviceId: req.service.id, scopes: req.service.scopes }, 'Service authenticated');
    next();
  } catch (error) {
    if (error instanceof jwt.TokenExpiredError) {
      logger.warn({ path: req.path }, 'Token expired');
      res.status(401).json({
        error: 'Unauthorized',
        message: 'Token has expired',
      });
      return;
    }

    if (error instanceof jwt.JsonWebTokenError) {
      logger.warn({ path: req.path, error: error.message }, 'Invalid token');
      res.status(401).json({
        error: 'Unauthorized',
        message: 'Invalid token',
      });
      return;
    }

    logger.error({ error }, 'Authentication error');
    res.status(401).json({
      error: 'Unauthorized',
      message: 'Authentication failed',
    });
  }
}

/**
 * Middleware to require specific scopes
 */
export function requireScope(...requiredScopes: Scope[]) {
  return (req: Request, res: Response, next: NextFunction): void => {
    if (!req.service) {
      res.status(401).json({
        error: 'Unauthorized',
        message: 'Service not authenticated',
      });
      return;
    }

    const hasRequiredScope = requiredScopes.some((scope) =>
      req.service!.scopes.includes(scope)
    );

    if (!hasRequiredScope) {
      logger.warn(
        { serviceId: req.service.id, required: requiredScopes, actual: req.service.scopes },
        'Insufficient scope'
      );
      res.status(403).json({
        error: 'Forbidden',
        message: `Required scope: ${requiredScopes.join(' or ')}`,
      });
      return;
    }

    next();
  };
}
