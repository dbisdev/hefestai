import { Router, type Request, type Response } from 'express';
import { authenticateService, requireScope, Scopes } from '../../middleware/index.js';
import { handleFlowError } from '../../common/index.js';
import { imageGenerateFlow } from './generate-image.flow.js';
import { ImageGenerateRequestSchema } from './generate-image.schema.js';
import { logger } from '../../logger.js';

const router = Router();

router.post(
  '/',
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
        logger.info({ serviceId: req.service?.id }, 'Image generation completed successfully');
      } else {
        logger.warn({ serviceId: req.service?.id, message: result.message }, 'Image generation failed');
      }

      res.json(result);
    } catch (error) {
      handleFlowError(error, res, 'generate-image');
    }
  }
);

export { router as imageGenerateRouter };
