import { Router, type Request, type Response } from 'express';
import { authenticateService, requireScope, Scopes } from '../../middleware/index.js';
import { handleFlowError } from '../../common/index.js';
import { chatFlow } from './chat.flow.js';
import { ChatRequestSchema } from './chat.schema.js';
import { logger } from '../../logger.js';

const router = Router();

router.post(
  '/',
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

export { router as chatRouter };
