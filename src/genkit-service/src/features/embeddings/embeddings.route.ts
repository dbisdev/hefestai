import { Router, type Request, type Response } from 'express';
import { authenticateService, requireScope, Scopes } from '../../middleware/index.js';
import { handleFlowError } from '../../common/index.js';
import { embeddingsFlow } from './embeddings.flow.js';
import { EmbeddingsRequestSchema } from './embeddings.schema.js';
import { logger } from '../../logger.js';

const router = Router();

router.post(
  '/',
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

export { router as embeddingsRouter };
