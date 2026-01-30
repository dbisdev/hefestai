import 'dotenv/config';
import express, { type Request, type Response, type NextFunction } from 'express';
import cors from 'cors';
import helmet from 'helmet';
import rateLimit from 'express-rate-limit';
import { pinoHttp } from 'pino-http';
import path from 'path';
import { fileURLToPath } from 'url';

// Initialize Genkit config and flows
import { chatFlow, generateTextFlow, summarizeFlow, embeddingsFlow, ragGenerateFlow, imageGenerateFlow } from './flows.js';
import { 
  ChatRequestSchema, 
  TextGenerationRequestSchema, 
  SummarizeRequestSchema,
  EmbeddingsRequestSchema,
  RagGenerateRequestSchema,
  ImageGenerateRequestSchema,
} from './schemas.js';
import { authenticateService, requireScope, Scopes } from './middleware/index.js';
import { logger } from './logger.js';

// ES module __dirname equivalent
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = process.env.PORT || 3000;
const NODE_ENV = process.env.NODE_ENV || 'development';

// Rate limiting configuration
const limiter = rateLimit({
  windowMs: parseInt(process.env.RATE_LIMIT_WINDOW_MS || '60000', 10),
  max: parseInt(process.env.RATE_LIMIT_MAX_REQUESTS || '100', 10),
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'Too many requests', message: 'Rate limit exceeded' },
});

// Middleware
app.use(helmet({
  contentSecurityPolicy: NODE_ENV === 'production',
}));
app.use(cors({
  origin: process.env.CORS_ORIGINS?.split(',') || ['http://localhost:5000', 'http://localhost:5173'],
  credentials: true,
}));
app.use(express.json({ limit: '1mb' }));
app.use(pinoHttp({ logger }));
app.use(limiter);

// Health check endpoint (no auth required)
app.get('/health', (_req: Request, res: Response) => {
  res.json({
    status: 'healthy',
    service: 'loremaster-genkit',
    version: '1.0.0',
    timestamp: new Date().toISOString(),
  });
});

// Favicon endpoint
app.get('/favicon.ico', (_req: Request, res: Response) => {
  res.sendFile(path.join(__dirname, '..', 'favicon.ico'));
});

// =============================================================================
// Protected AI Endpoints - Require service authentication
// =============================================================================

// Generate endpoint
app.post(
  '/api/generate',
  authenticateService,
  requireScope(Scopes.GENKIT_EXECUTE),
  async (req: Request, res: Response) => {
    try {
      const validatedInput = TextGenerationRequestSchema.parse(req.body);
      logger.info({ serviceId: req.service?.id, operation: 'generate' }, 'Processing generate request');
      
      const result = await generateTextFlow(validatedInput);
      logger.info({ serviceId: req.service?.id, operation: result.text }, 'Resultado generate request');
      res.json(result);
    } catch (error) {
      handleFlowError(error, res, 'generate');
    }
  }
);

// Chat endpoint
app.post(
  '/api/chat',
  authenticateService,
  requireScope(Scopes.GENKIT_EXECUTE),
  async (req: Request, res: Response) => {
    try {
      const validatedInput = ChatRequestSchema.parse(req.body);
      logger.info({ serviceId: req.service?.id, operation: 'chat' }, 'Processing chat request');
      
      const result = await chatFlow(validatedInput);
      res.json(result);
    } catch (error) {
      handleFlowError(error, res, 'chat');
    }
  }
);

// Summarize endpoint
app.post(
  '/api/summarize',
  authenticateService,
  requireScope(Scopes.GENKIT_EXECUTE),
  async (req: Request, res: Response) => {
    try {
      const validatedInput = SummarizeRequestSchema.parse(req.body);
      logger.info(
        { serviceId: req.service?.id, operation: 'summarize', inputLength: validatedInput.text.length },
        'Processing summarize request'
      );
      
      const result = await summarizeFlow(validatedInput);
      res.json(result);
    } catch (error) {
      handleFlowError(error, res, 'summarize');
    }
  }
);

// =============================================================================
// Embeddings endpoint (for RAG)
// =============================================================================
app.post(
  '/api/embeddings',
  authenticateService,
  requireScope(Scopes.GENKIT_EXECUTE),
  async (req: Request, res: Response) => {
    try {
      const validatedInput = EmbeddingsRequestSchema.parse(req.body);
      logger.info(
        { serviceId: req.service?.id, operation: 'embeddings', textCount: validatedInput.texts.length },
        'Processing embeddings request'
      );
      
      const result = await embeddingsFlow(validatedInput);
      res.json(result);
    } catch (error) {
      handleFlowError(error, res, 'embeddings');
    }
  }
);

// =============================================================================
// RAG Generate endpoint
// =============================================================================
app.post(
  '/api/rag/generate',
  authenticateService,
  requireScope(Scopes.GENKIT_EXECUTE),
  async (req: Request, res: Response) => {
    try {
      const validatedInput = RagGenerateRequestSchema.parse(req.body);
      logger.info(
        { serviceId: req.service?.id, operation: 'rag-generate', contextCount: validatedInput.context.length },
        'Processing RAG generate request'
      );
      
      const result = await ragGenerateFlow(validatedInput);
      res.json(result);
    } catch (error) {
      handleFlowError(error, res, 'rag-generate');
    }
  }
);

// =============================================================================
// Image Generation endpoint
// =============================================================================
app.post(
  '/api/generate-image',
  authenticateService,
  requireScope(Scopes.GENKIT_EXECUTE),
  async (req: Request, res: Response) => {
    try {
      const validatedInput = ImageGenerateRequestSchema.parse(req.body);
      logger.info(
        { 
          serviceId: req.service?.id, 
          operation: 'generate-image', 
          promptLength: validatedInput.prompt.length,
          style: validatedInput.style,
          aspectRatio: validatedInput.aspectRatio,
        },
        'Processing image generation request'
      );
      
      const result = await imageGenerateFlow(validatedInput);
      
      if (result.success) {
        logger.info({ serviceId: req.service?.id, message: result.image?.base64 }, 'Image generation completed successfully');
      } else {
        logger.warn({ serviceId: req.service?.id, message: result.message }, 'Image generation failed');
      }
      
      res.json(result);
    } catch (error) {
      handleFlowError(error, res, 'generate-image');
    }
  }
);

// =============================================================================
// Error Handling
// =============================================================================

function handleFlowError(error: unknown, res: Response, operation: string): void {
  if (error instanceof Error && error.name === 'ZodError') {
    logger.warn({ operation, error: (error as any).errors }, 'Validation error');
    res.status(400).json({
      error: 'Validation error',
      details: (error as any).errors,
    });
    return;
  }

  logger.error({ operation, error }, 'Flow execution error');
  res.status(500).json({
    error: 'Internal server error',
    message: NODE_ENV === 'development' && error instanceof Error ? error.message : 'An error occurred',
  });
}

// Global error handling middleware
app.use((err: Error, _req: Request, res: Response, _next: NextFunction) => {
  logger.error({ error: err }, 'Unhandled error');
  res.status(500).json({
    error: 'Internal server error',
    message: NODE_ENV === 'development' ? err.message : 'An unexpected error occurred',
  });
});

// 404 handler
app.use((_req: Request, res: Response) => {
  res.status(404).json({ error: 'Not found' });
});

// Start server
app.listen(PORT, () => {
  logger.info({ port: PORT, env: NODE_ENV }, 'Genkit service started');
  logger.info(`Health check: http://localhost:${PORT}/health`);
});

export default app;
