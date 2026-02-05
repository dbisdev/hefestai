/**
 * Admin Service
 * Single Responsibility: Admin CRUD operations for users and campaigns.
 * All operations require Admin role authentication.
 */

import { httpClient } from './client';
import type {
  AdminUser,
  AdminCampaign,
  CreateUserRequest,
  UpdateUserRequest,
  AdminUpdateCampaignRequest,
} from '../../types';

/**
 * Service for admin user management operations.
 * All endpoints require Admin role.
 */
export const adminUserService = {
  /**
   * Get all users (Admin only).
   * Backend endpoint: GET /api/admin/users
   * @param includeInactive - Include inactive users (default: false)
   */
  async getAll(includeInactive = false): Promise<AdminUser[]> {
    const params = includeInactive ? '?includeInactive=true' : '';
    return httpClient.get<AdminUser[]>(`/admin/users${params}`);
  },

  /**
   * Get a user by ID (Admin only).
   * Backend endpoint: GET /api/admin/users/{id}
   * @param id - User ID
   */
  async getById(id: string): Promise<AdminUser> {
    return httpClient.get<AdminUser>(`/admin/users/${id}`);
  },

  /**
   * Create a new user (Admin only).
   * Backend endpoint: POST /api/admin/users
   * @param request - User creation data
   */
  async create(request: CreateUserRequest): Promise<AdminUser> {
    return httpClient.post<AdminUser>('/admin/users', request);
  },

  /**
   * Update a user (Admin only).
   * Backend endpoint: PUT /api/admin/users/{id}
   * @param id - User ID
   * @param request - User update data
   */
  async update(id: string, request: UpdateUserRequest): Promise<AdminUser> {
    return httpClient.put<AdminUser>(`/admin/users/${id}`, request);
  },

  /**
   * Delete a user (Admin only). Performs soft delete.
   * Backend endpoint: DELETE /api/admin/users/{id}
   * @param id - User ID
   */
  async delete(id: string): Promise<void> {
    return httpClient.delete(`/admin/users/${id}`);
  },
};

/**
 * Service for admin campaign management operations.
 * All endpoints require Admin role.
 */
export const adminCampaignService = {
  /**
   * Get all campaigns (Admin only).
   * Backend endpoint: GET /api/admin/campaigns
   * @param includeInactive - Include inactive campaigns (default: false)
   */
  async getAll(includeInactive = false): Promise<AdminCampaign[]> {
    const params = includeInactive ? '?includeInactive=true' : '';
    return httpClient.get<AdminCampaign[]>(`/admin/campaigns${params}`);
  },

  /**
   * Get a campaign by ID (Admin only).
   * Backend endpoint: GET /api/admin/campaigns/{id}
   * @param id - Campaign ID
   */
  async getById(id: string): Promise<AdminCampaign> {
    return httpClient.get<AdminCampaign>(`/admin/campaigns/${id}`);
  },

  /**
   * Update a campaign (Admin only).
   * Backend endpoint: PUT /api/admin/campaigns/{id}
   * @param id - Campaign ID
   * @param request - Campaign update data
   */
  async update(id: string, request: AdminUpdateCampaignRequest): Promise<AdminCampaign> {
    return httpClient.put<AdminCampaign>(`/admin/campaigns/${id}`, request);
  },

  /**
   * Delete a campaign (Admin only). Performs soft delete.
   * Backend endpoint: DELETE /api/admin/campaigns/{id}
   * @param id - Campaign ID
   */
  async delete(id: string): Promise<void> {
    return httpClient.delete(`/admin/campaigns/${id}`);
  },
};
