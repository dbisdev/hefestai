/**
 * Central API service exports
 */

export { httpClient, apiRequest, ApiRequestError } from './client';
export { authService } from './auth.service';
export { entityService } from './entity.service';
export { campaignService } from './campaign.service';
export { gameSystemService } from './gameSystem.service';
export { entityTemplateService } from './entityTemplate.service';
export { adminUserService, adminCampaignService } from './admin.service';
export { aiService } from './ai.service';
export { documentService } from './document.service';
export type { GeneratedCharacter, GeneratedSolarSystem, GeneratedVehicle } from './ai.service';
export type { 
  DocumentDto, 
  DocumentSearchResult, 
  SemanticSearchParams, 
  SemanticSearchResult,
  ManualDto,
  ManualSummaryDto,
  UploadManualResult,
  RagSourceType,
  DocumentAvailabilityResult
} from './document.service';
export type { 
  PaginatedEntitiesResult, 
  GetEntitiesOptions 
} from './entity.service';
