/**
 * Campaign Types
 * Aligned with backend Campaign DTOs
 * Single Responsibility: Only campaign-related type definitions
 */

/**
 * Campaign role enum matching backend CampaignRole.
 * Uses string values because the backend serializes all enums as their
 * PascalCase string names via JsonStringEnumConverter (Program.cs).
 * The API sends "Master" / "Player" — not 0 / 1.
 */
export enum CampaignRole {
  Player = 'Player',
  Master = 'Master'
}

/**
 * Game system summary for embedded display
 */
export interface GameSystemSummary {
  id: string;
  name: string;
}

/**
 * Settings object for a campaign (game rules, house rules, etc.)
 * Extensible for future game system-specific settings.
 */
export interface CampaignSettings {
  /** Maximum number of players allowed */
  maxPlayers?: number;
  /** Whether players can see other players' sheets */
  sharedSheets?: boolean;
  /** Any additional settings keyed by string */
  [key: string]: string | number | boolean | undefined;
}

/**
 * Campaign summary DTO matching backend CampaignDto
 */
export interface Campaign {
  id: string;
  name: string;
  description?: string;
  /** The user ID of the campaign owner (Master who created it). */
  ownerId: string;
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
  joinCode?: string;  // Only visible to Masters
  settings?: CampaignSettings;
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
  settings?: CampaignSettings;
}

/**
 * Update campaign request matching backend UpdateCampaignRequest
 */
export interface UpdateCampaignInput {
  name: string;
  description?: string;
  settings?: CampaignSettings;
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
