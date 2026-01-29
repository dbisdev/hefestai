/**
 * Central API service exports
 */

export { httpClient, apiRequest, ApiRequestError } from './client';
export { authService } from './auth.service';
export { entityService } from './entity.service';
export { campaignService } from './campaign.service';
export { gameSystemService } from './gameSystem.service';
export { aiService } from './ai.service';
export type { GeneratedCharacter, GeneratedSolarSystem, GeneratedVehicle } from './ai.service';
