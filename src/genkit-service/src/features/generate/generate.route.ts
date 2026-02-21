import { Router, type Request, type Response } from 'express';
import { authenticateService, requireScope, Scopes } from '../../middleware/index.js';
import { handleFlowError } from '../../common/index.js';
import { generateTextFlow } from './generate.flow.js';
import { TextGenerationRequestSchema } from './generate.schema.js';
import { logger } from '../../logger.js';

const router = Router();

router.post(
  '/',
  authenticateService,
  requireScope(Scopes.GENKIT_EXECUTE),
  async (req: Request, res: Response) => {
    try {
      const validatedInput = TextGenerationRequestSchema.parse(req.body);
      logger.info({ serviceId: req.service?.id, operation: 'generate' }, 'Processing generate request');

      const result = await generateTextFlow(validatedInput);
      res.json(result);
    } catch (error) {
      handleFlowError(error, res, 'generate');
    }
  }
);

export { router as generateRouter };
