import { Router, type Request, type Response } from 'express';
import { authenticateService, requireScope, Scopes } from '../../middleware/index.js';
import { handleFlowError } from '../../common/index.js';
import { ragGenerateFlow } from './rag-generate.flow.js';
import { RagGenerateRequestSchema } from './rag-generate.schema.js';
import { logger } from '../../logger.js';

const router = Router();

router.post(
  '/',
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

export { router as ragGenerateRouter };
