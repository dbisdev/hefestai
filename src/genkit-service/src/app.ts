import 'dotenv/config';
import express, { type Request, type Response } from 'express';
import path from 'path';
import { fileURLToPath } from 'url';
import { configureMiddleware } from './config/index.js';
import { errorHandler, notFoundHandler } from './common/index.js';
import {
  generateRouter,
  chatRouter,
  summarizeRouter,
  embeddingsRouter,
  ragGenerateRouter,
  imageGenerateRouter,
} from './features/index.js';
import { logger } from './logger.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export function createApp() {
  const app = express();

  configureMiddleware(app);

  app.get('/health', (_req: Request, res: Response) => {
    res.json({
      status: 'healthy',
      service: 'loremaster-genkit',
      version: '1.0.0',
      timestamp: new Date().toISOString(),
    });
  });

  app.get('/favicon.ico', (_req: Request, res: Response) => {
    res.sendFile(path.join(__dirname, '..', 'favicon.ico'));
  });

  app.use('/api/generate', generateRouter);
  app.use('/api/chat', chatRouter);
  app.use('/api/summarize', summarizeRouter);
  app.use('/api/embeddings', embeddingsRouter);
  app.use('/api/rag/generate', ragGenerateRouter);
  app.use('/api/generate-image', imageGenerateRouter);

  app.use(errorHandler);
  app.use(notFoundHandler);

  return app;
}

export function startServer(port?: number) {
  const PORT = port || parseInt(process.env.PORT || '3000', 10);
  const NODE_ENV = process.env.NODE_ENV || 'development';

  const app = createApp();

  app.listen(PORT, () => {
    logger.info({ port: PORT, env: NODE_ENV }, 'Genkit service started');
    logger.info(`Health check: http://localhost:${PORT}/health`);
  });

  return app;
}
