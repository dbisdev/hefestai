import { Router, type Request, type Response } from 'express';
import { authenticateService, requireScope, Scopes } from '../../middleware/index.js';
import { handleFlowError } from '../../common/index.js';
import { summarizeFlow } from './summarize.flow.js';
import { SummarizeRequestSchema } from './summarize.schema.js';
import { logger } from '../../logger.js';

const router = Router();

router.post(
  '/',
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

export { router as summarizeRouter };
