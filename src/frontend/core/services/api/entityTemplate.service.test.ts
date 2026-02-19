/**
 * Entity Template Service Unit Tests
 * Tests for entity template CRUD and extraction operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { entityTemplateService } from './entityTemplate.service';
import { httpClient } from './client';
import { TemplateStatus, FieldType } from '@core/types';

vi.mock('./client', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Entity Template Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getByGameSystem', () => {
    it('fetches templates for a game system', async () => {
      const mockResult = {
        templates: [{ id: '1', entityTypeName: 'character' }],
        totalCount: 1,
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.getByGameSystem('system-1');
      
      expect(httpClient.get).toHaveBeenCalledWith('/game-systems/system-1/templates');
      expect(result).toEqual(mockResult);
    });

    it('includes status filter', async () => {
      const mockResult = { templates: [], totalCount: 0 };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      await entityTemplateService.getByGameSystem('system-1', TemplateStatus.Confirmed);
      
      const callUrl = vi.mocked(httpClient.get).mock.calls[0][0];
      expect(callUrl).toContain('status=2');
    });

    it('includes confirmedOnly filter', async () => {
      const mockResult = { templates: [], totalCount: 0 };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      await entityTemplateService.getByGameSystem('system-1', undefined, true);
      
      const callUrl = vi.mocked(httpClient.get).mock.calls[0][0];
      expect(callUrl).toContain('confirmedOnly=true');
    });
  });

  describe('getById', () => {
    it('fetches template by ID', async () => {
      const mockTemplate = {
        id: 'template-1',
        entityTypeName: 'character',
        fields: [],
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockTemplate);
      
      const result = await entityTemplateService.getById('system-1', 'template-1');
      
      expect(httpClient.get).toHaveBeenCalledWith(
        '/game-systems/system-1/templates/template-1'
      );
      expect(result).toEqual(mockTemplate);
    });
  });

  describe('getConfirmedByEntityType', () => {
    it('returns matching template', async () => {
      const mockResult = {
        templates: [{ id: 'template-1', entityTypeName: 'character', status: TemplateStatus.Confirmed }],
        totalCount: 1,
      };
      const mockTemplate = {
        id: 'template-1',
        entityTypeName: 'character',
        fields: [{ name: 'STR' }],
      };
      vi.mocked(httpClient.get)
        .mockResolvedValueOnce(mockResult)
        .mockResolvedValueOnce(mockTemplate);
      
      const result = await entityTemplateService.getConfirmedByEntityType('system-1', 'character');
      
      expect(result).toEqual(mockTemplate);
    });

    it('normalizes entity type name', async () => {
      const mockResult = {
        templates: [{ id: 'template-1', entityTypeName: 'solar_system', status: TemplateStatus.Confirmed }],
        totalCount: 1,
      };
      const mockTemplate = { id: 'template-1', entityTypeName: 'solar_system', fields: [] };
      vi.mocked(httpClient.get)
        .mockResolvedValueOnce(mockResult)
        .mockResolvedValueOnce(mockTemplate);
      
      const result = await entityTemplateService.getConfirmedByEntityType('system-1', 'Solar System');
      
      expect(result).toEqual(mockTemplate);
    });

    it('returns null when not found', async () => {
      const mockResult = { templates: [], totalCount: 0 };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.getConfirmedByEntityType('system-1', 'nonexistent');
      
      expect(result).toBeNull();
    });

    it('returns null on error', async () => {
      vi.mocked(httpClient.get).mockRejectedValueOnce(new Error('Failed'));
      
      const result = await entityTemplateService.getConfirmedByEntityType('system-1', 'character');
      
      expect(result).toBeNull();
    });
  });

  describe('getFieldDefinitions', () => {
    it('returns field definitions for entity type', async () => {
      const mockResult = {
        templates: [{ id: 'template-1', entityTypeName: 'character', status: TemplateStatus.Confirmed }],
        totalCount: 1,
      };
      const mockTemplate = {
        id: 'template-1',
        fields: [{ name: 'STR', displayName: 'Strength' }],
      };
      vi.mocked(httpClient.get)
        .mockResolvedValueOnce(mockResult)
        .mockResolvedValueOnce(mockTemplate);
      
      const result = await entityTemplateService.getFieldDefinitions('system-1', 'character');
      
      expect(result).toEqual([{ name: 'STR', displayName: 'Strength' }]);
    });

    it('returns empty array when not found', async () => {
      const mockResult = { templates: [], totalCount: 0 };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.getFieldDefinitions('system-1', 'nonexistent');
      
      expect(result).toEqual([]);
    });
  });

  describe('extractFromManual', () => {
    it('extracts templates without source document', async () => {
      const mockResult = { extracted: 5, templates: [] };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.extractFromManual('system-1');
      
      expect(httpClient.post).toHaveBeenCalledWith(
        '/game-systems/system-1/templates/extract',
        {}
      );
      expect(result).toEqual(mockResult);
    });

    it('extracts templates with source document', async () => {
      const mockResult = { extracted: 3, templates: [] };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.extractFromManual('system-1', 'doc-1');
      
      expect(httpClient.post).toHaveBeenCalledWith(
        '/game-systems/system-1/templates/extract',
        { sourceDocumentId: 'doc-1' }
      );
      expect(result).toEqual(mockResult);
    });
  });

  describe('create', () => {
    it('creates a template', async () => {
      const mockResult = { id: 'new-template', success: true };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.create('system-1', {
        entityTypeName: 'character',
        displayName: 'Character',
        fields: [],
      });
      
      expect(httpClient.post).toHaveBeenCalledWith(
        '/game-systems/system-1/templates',
        { entityTypeName: 'character', displayName: 'Character', fields: [] }
      );
      expect(result).toEqual(mockResult);
    });
  });

  describe('update', () => {
    it('updates a template', async () => {
      const mockResult = { id: 'template-1', success: true };
      vi.mocked(httpClient.put).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.update('system-1', 'template-1', {
        displayName: 'Updated Template',
        fields: [{ name: 'STR', displayName: 'Strength', fieldType: FieldType.Number, isRequired: false, order: 0 }],
      });
      
      expect(httpClient.put).toHaveBeenCalledWith(
        '/game-systems/system-1/templates/template-1',
        { displayName: 'Updated Template', fields: [{ name: 'STR', displayName: 'Strength', fieldType: FieldType.Number, isRequired: false, order: 0 }] }
      );
      expect(result).toEqual(mockResult);
    });
  });

  describe('confirm', () => {
    it('confirms a template without notes', async () => {
      const mockResult = { id: 'template-1', status: TemplateStatus.Confirmed };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.confirm('system-1', 'template-1');
      
      expect(httpClient.post).toHaveBeenCalledWith(
        '/game-systems/system-1/templates/template-1/confirm',
        {}
      );
      expect(result).toEqual(mockResult);
    });

    it('confirms a template with notes', async () => {
      const mockResult = { id: 'template-1', status: TemplateStatus.Confirmed };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.confirm('system-1', 'template-1', {
        notes: 'Verified',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith(
        '/game-systems/system-1/templates/template-1/confirm',
        { notes: 'Verified' }
      );
      expect(result).toEqual(mockResult);
    });
  });

  describe('delete', () => {
    it('deletes a template', async () => {
      const mockResult = { success: true };
      vi.mocked(httpClient.delete).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.delete('system-1', 'template-1');
      
      expect(httpClient.delete).toHaveBeenCalledWith(
        '/game-systems/system-1/templates/template-1'
      );
      expect(result).toEqual(mockResult);
    });

    it('forces deletion', async () => {
      const mockResult = { success: true };
      vi.mocked(httpClient.delete).mockResolvedValueOnce(mockResult);
      
      const result = await entityTemplateService.delete('system-1', 'template-1', true);
      
      expect(httpClient.delete).toHaveBeenCalledWith(
        '/game-systems/system-1/templates/template-1?force=true'
      );
      expect(result).toEqual(mockResult);
    });
  });

  describe('confirmAll', () => {
    it('confirms all confirmable templates', async () => {
      const mockResult = {
        templates: [
          { id: 't1', entityTypeName: 'a', status: TemplateStatus.Draft },
          { id: 't2', entityTypeName: 'b', status: TemplateStatus.PendingReview },
          { id: 't3', entityTypeName: 'c', status: TemplateStatus.Confirmed },
        ],
        totalCount: 3,
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      vi.mocked(httpClient.post)
        .mockResolvedValueOnce({})
        .mockResolvedValueOnce({});
      
      const result = await entityTemplateService.confirmAll('system-1');
      
      expect(result).toEqual({ confirmed: 2, failed: 0 });
    });

    it('counts failures', async () => {
      const mockResult = {
        templates: [
          { id: 't1', entityTypeName: 'a', status: TemplateStatus.Draft },
          { id: 't2', entityTypeName: 'b', status: TemplateStatus.Draft },
        ],
        totalCount: 2,
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      vi.mocked(httpClient.post)
        .mockResolvedValueOnce({})
        .mockRejectedValueOnce(new Error('Failed'));
      
      const result = await entityTemplateService.confirmAll('system-1');
      
      expect(result).toEqual({ confirmed: 1, failed: 1 });
    });
  });
});
