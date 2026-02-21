import express, { type Express } from 'express';
import cors from 'cors';
import helmet from 'helmet';
import rateLimit from 'express-rate-limit';
import { pinoHttp } from 'pino-http';
import { logger } from '../logger.js';

const NODE_ENV = process.env.NODE_ENV || 'development';

export function configureMiddleware(app: Express): void {
  app.use(helmet({
    contentSecurityPolicy: NODE_ENV === 'production',
  }));

  app.use(cors({
    origin: process.env.CORS_ORIGINS?.split(',') || ['http://localhost:5000', 'http://localhost:5173'],
    credentials: true,
  }));

  app.use(express.json({ limit: '1mb' }));

  app.use(pinoHttp({ logger }));

  const limiter = rateLimit({
    windowMs: parseInt(process.env.RATE_LIMIT_WINDOW_MS || '60000', 10),
    max: parseInt(process.env.RATE_LIMIT_MAX_REQUESTS || '100', 10),
    standardHeaders: true,
    legacyHeaders: false,
    message: { error: 'Too many requests', message: 'Rate limit exceeded' },
  });
  app.use(limiter);
}
