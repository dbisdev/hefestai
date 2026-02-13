/**
 * Campaign Types
 * Aligned with backend Campaign DTOs
 * Single Responsibility: Only campaign-related type definitions
 */

/**
 * Campaign role enum matching backend CampaignRole
 */
export enum CampaignRole {
  Player = 0,
  Master = 1
}

/**
 * Game system summary for embedded display
 */
export interface GameSystemSummary {
  id: string;
  name: string;
}

/**
 * Campaign summary DTO matching backend CampaignDto
 */
export interface Campaign {
  id: string;
  name: string;
  description?: string;
  gameSystemId: string;
  /** Game system details (when included in response) */
  gameSystem?: GameSystemSummary;
  /** Join code for the campaign (visible to Masters) */
  joinCode?: string;
  isActive: boolean;
  userRole?: CampaignRole;
  createdAt: string;
}

/**
 * Detailed campaign DTO matching backend CampaignDetailDto
 */
export interface CampaignDetail extends Campaign {
  ownerId: string;
  joinCode?: string;  // Only visible to Masters
  settings?: Record<string, unknown>;
  memberCount: number;
  updatedAt?: string;
}

/**
 * Campaign member DTO matching backend CampaignMemberDto
 */
export interface CampaignMember {
  id: string;
  userId: string;
  displayName: string;
  role: CampaignRole;
  joinedAt: string;
}

/**
 * Create campaign request matching backend CreateCampaignRequest
 */
export interface CreateCampaignInput {
  name: string;
  description?: string;
  gameSystemId: string;
  settings?: Record<string, unknown>;
}

/**
 * Update campaign request matching backend UpdateCampaignRequest
 */
export interface UpdateCampaignInput {
  name: string;
  description?: string;
  settings?: Record<string, unknown>;
}

/**
 * Join campaign request matching backend JoinCampaignRequest
 */
export interface JoinCampaignInput {
  joinCode: string;
}

/**
 * Update member role request matching backend UpdateMemberRoleRequest
 */
export interface UpdateMemberRoleInput {
  role: CampaignRole;
}

/**
 * Update campaign status request matching backend UpdateCampaignStatusRequest
 */
export interface UpdateCampaignStatusInput {
  isActive: boolean;
}

/**
 * Join code response matching backend JoinCodeResponse
 */
export interface JoinCodeResponse {
  joinCode: string;
}
