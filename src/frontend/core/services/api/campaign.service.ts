/**
 * Campaign Service
 * Single Responsibility: Campaign CRUD and membership operations
 */

import { httpClient } from './client';
import type {
  Campaign,
  CampaignDetail,
  CampaignMember,
  CreateCampaignInput,
  UpdateCampaignInput,
  JoinCampaignInput,
  UpdateMemberRoleInput,
  UpdateCampaignStatusInput,
  JoinCodeResponse,
} from '@core/types';

export const campaignService = {
  /**
   * Get all campaigns the current user is a member of
   */
  async getAll(): Promise<Campaign[]> {
    return httpClient.get<Campaign[]>('/campaigns');
  },

  /**
   * Get a specific campaign by ID with full details
   */
  async getById(id: string): Promise<CampaignDetail> {
    return httpClient.get<CampaignDetail>(`/campaigns/${id}`);
  },

  /**
   * Create a new campaign (current user becomes Master)
   */
  async create(input: CreateCampaignInput): Promise<CampaignDetail> {
    return httpClient.post<CampaignDetail>('/campaigns', input);
  },

  /**
   * Update campaign details (Master only)
   */
  async update(id: string, input: UpdateCampaignInput): Promise<CampaignDetail> {
    return httpClient.put<CampaignDetail>(`/campaigns/${id}`, input);
  },

  /**
   * Delete a campaign (Owner only)
   */
  async delete(id: string): Promise<void> {
    await httpClient.delete(`/campaigns/${id}`);
  },

  /**
   * Join a campaign using a join code
   */
  async joinByCode(input: JoinCampaignInput): Promise<Campaign> {
    return httpClient.post<Campaign>('/campaigns/join', input);
  },

  /**
   * Leave a campaign
   */
  async leave(campaignId: string): Promise<void> {
    await httpClient.post(`/campaigns/${campaignId}/leave`, {});
  },

  /**
   * Get campaign members
   */
  async getMembers(campaignId: string): Promise<CampaignMember[]> {
    return httpClient.get<CampaignMember[]>(`/campaigns/${campaignId}/members`);
  },

  /**
   * Update a member's role (Master only)
   */
  async updateMemberRole(
    campaignId: string,
    memberId: string,
    input: UpdateMemberRoleInput
  ): Promise<CampaignMember> {
    return httpClient.patch<CampaignMember>(
      `/campaigns/${campaignId}/members/${memberId}/role`,
      input
    );
  },

  /**
   * Remove a member from the campaign (Master only)
   */
  async removeMember(campaignId: string, memberId: string): Promise<void> {
    await httpClient.delete(`/campaigns/${campaignId}/members/${memberId}`);
  },

  /**
   * Regenerate campaign join code (Master only)
   */
  async regenerateJoinCode(campaignId: string): Promise<JoinCodeResponse> {
    return httpClient.post<JoinCodeResponse>(
      `/campaigns/${campaignId}/regenerate-code`,
      {}
    );
  },

  /**
   * Update campaign status (activate/deactivate) (Master only)
   */
  async updateStatus(
    campaignId: string,
    input: UpdateCampaignStatusInput
  ): Promise<Campaign> {
    return httpClient.patch<Campaign>(`/campaigns/${campaignId}/status`, input);
  },
};
