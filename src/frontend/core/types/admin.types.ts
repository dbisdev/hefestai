/**
 * Admin Types
 * Types for admin management operations matching backend DTOs.
 * Used for user and campaign administration (Admin role only).
 */

/**
 * Numeric user role enum matching backend UserRole.
 * Used for API communication in admin operations.
 * NOTE: The frontend uses string roles ('PLAYER'|'MASTER'|'ADMIN') internally.
 */
export enum AdminUserRole {
  Player = 0,
  Master = 1,
  Admin = 2,
}

/**
 * Human-readable labels for admin user roles (Spanish).
 */
export const AdminUserRoleLabels: Record<AdminUserRole, string> = {
  [AdminUserRole.Player]: 'Jugador',
  [AdminUserRole.Master]: 'Master',
  [AdminUserRole.Admin]: 'Administrador',
};

/**
 * CSS color classes for admin user role badges.
 */
export const AdminUserRoleColors: Record<AdminUserRole, string> = {
  [AdminUserRole.Player]: 'border-green-500 text-green-500 bg-green-500/10',
  [AdminUserRole.Master]: 'border-yellow-500 text-yellow-500 bg-yellow-500/10',
  [AdminUserRole.Admin]: 'border-red-500 text-red-500 bg-red-500/10',
};

/**
 * Admin user DTO matching backend AdminUserDto.
 * Contains full user information visible to administrators.
 */
export interface AdminUser {
  /** User unique identifier. */
  id: string;
  /** User email address. */
  email: string;
  /** User display name. */
  displayName: string | null;
  /** User role (Player=0, Master=1, Admin=2). */
  role: AdminUserRole;
  /** User avatar URL. */
  avatarUrl: string | null;
  /** Whether the user account is active. */
  isActive: boolean;
  /** Last login timestamp (ISO string). */
  lastLoginAt: string | null;
  /** Account creation timestamp (ISO string). */
  createdAt: string;
  /** Last update timestamp (ISO string). */
  updatedAt: string | null;
  /** Number of campaigns owned by this user. */
  ownedCampaignsCount: number;
  /** Number of campaign memberships. */
  campaignMembershipsCount: number;
}

/**
 * Admin campaign DTO matching backend AdminCampaignDto.
 * Contains full campaign information visible to administrators.
 */
export interface AdminCampaign {
  /** Campaign unique identifier. */
  id: string;
  /** Campaign name. */
  name: string;
  /** Campaign description. */
  description: string | null;
  /** Join code for players to join the campaign. */
  joinCode: string;
  /** Whether the campaign is active. */
  isActive: boolean;
  /** Owner/creator user ID. */
  ownerId: string;
  /** Owner display name or email. */
  ownerName: string;
  /** Game system ID. */
  gameSystemId: string;
  /** Game system name. */
  gameSystemName: string;
  /** Number of members in the campaign. */
  memberCount: number;
  /** Number of lore entities in the campaign. */
  entityCount: number;
  /** Campaign creation timestamp (ISO string). */
  createdAt: string;
  /** Last update timestamp (ISO string). */
  updatedAt: string | null;
}

/**
 * Request to create a new user (Admin only).
 */
export interface CreateUserRequest {
  /** User email address. */
  email: string;
  /** User password. */
  password: string;
  /** Optional display name. */
  displayName?: string;
  /** User role (defaults to Player). */
  role?: AdminUserRole;
  /** Whether the user is active (defaults to true). */
  isActive?: boolean;
}

/**
 * Request to update a user (Admin only).
 * All fields are optional - only provided fields will be updated.
 */
export interface UpdateUserRequest {
  /** New email (optional). */
  email?: string;
  /** New password (optional). */
  password?: string;
  /** New display name (optional). */
  displayName?: string;
  /** New role (optional). */
  role?: AdminUserRole;
  /** New active status (optional). */
  isActive?: boolean;
}

/**
 * Request to update a campaign (Admin only).
 * All fields are optional - only provided fields will be updated.
 */
export interface AdminUpdateCampaignRequest {
  /** New campaign name (optional). */
  name?: string;
  /** New description (optional). */
  description?: string;
  /** New active status (optional). */
  isActive?: boolean;
  /** New owner ID (optional, for ownership transfer). */
  ownerId?: string;
}
