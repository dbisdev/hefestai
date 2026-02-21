/**
 * Tests for useEntityGeneration hook
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useEntityGeneration } from './useEntityGeneration';
import type { UseEntityGenerationConfig, GenerationResult } from './useEntityGeneration.types';

interface TestEntity {
  name: string;
  description: string;
}

const createMockConfig = (): UseEntityGenerationConfig<TestEntity> => ({
  entityType: 'character',
  placeholderImage: 'https://placeholder.com/image.jpg',
  initialLogs: ['> System initialized'],
  maxLogs: 6,
  generateFn: vi.fn(),
  saveFn: vi.fn(),
  getFieldDefinitions: vi.fn(),
  onGenerationSuccess: vi.fn(),
  onGenerationError: vi.fn(),
  onSaveSuccess: vi.fn(),
  onSaveError: vi.fn(),
});

describe('useEntityGeneration', () => {
  let mockConfig: UseEntityGenerationConfig<TestEntity>;

  beforeEach(() => {
    vi.clearAllMocks();
    mockConfig = createMockConfig();
  });

  describe('initial state', () => {
    it('should initialize with default values', () => {
      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      expect(result.current.isGenerating).toBe(false);
      expect(result.current.isSaving).toBe(false);
      expect(result.current.generatedData).toBeNull();
      expect(result.current.editableData).toBeNull();
      expect(result.current.image).toBe(mockConfig.placeholderImage);
      expect(result.current.generationRequestId).toBeUndefined();
      expect(result.current.imageMode).toBe('generate');
      expect(result.current.uploadedImageData).toBeNull();
      expect(result.current.logs).toEqual(['> System initialized']);
    });

    it('should use custom initial logs', () => {
      const customLogs = ['> Custom log 1', '> Custom log 2'];
      mockConfig.initialLogs = customLogs;
      
      const { result } = renderHook(() => useEntityGeneration(mockConfig));
      
      expect(result.current.logs).toEqual(customLogs);
    });
  });

  describe('generate', () => {
    it('should call generateFn with correct parameters', async () => {
      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: { name: 'Test Character', description: 'Test description' },
        generationRequestId: 'req-123',
      } as GenerationResult<TestEntity>);
      mockConfig.generateFn = mockGenerateFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.generate({ species: 'human', role: 'marine' });
      });

      expect(mockGenerateFn).toHaveBeenCalledWith(
        { species: 'human', role: 'marine' },
        true // generateImage = true by default (imageMode is 'generate')
      );
      expect(result.current.generatedData).toEqual({
        name: 'Test Character',
        description: 'Test description',
      });
      expect(result.current.generationRequestId).toBe('req-123');
      expect(result.current.isGenerating).toBe(false);
    });

    it('should use uploaded image when imageMode is upload', async () => {
      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: { name: 'Test', description: 'Desc' },
      } as GenerationResult<TestEntity>);
      mockConfig.generateFn = mockGenerateFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      act(() => {
        result.current.setImageMode('upload');
        result.current.setUploadedImageData('base64imagedata');
      });

      await act(async () => {
        await result.current.generate({});
      });

      expect(mockGenerateFn).toHaveBeenCalledWith({}, false);
      expect(result.current.image).toBe('data:image/webp;base64,base64imagedata');
    });

    it('should handle base64 image from generation', async () => {
      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: { name: 'Test', description: 'Desc' },
        imageBase64: 'generatedimagebase64',
      } as GenerationResult<TestEntity>);
      mockConfig.generateFn = mockGenerateFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.generate({});
      });

      expect(result.current.image).toBe('data:image/webp;base64,generatedimagebase64');
    });

    it('should handle imageUrl from generation', async () => {
      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: { name: 'Test', description: 'Desc' },
        imageUrl: 'https://example.com/image.jpg',
      } as GenerationResult<TestEntity>);
      mockConfig.generateFn = mockGenerateFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.generate({});
      });

      expect(result.current.image).toBe('https://example.com/image.jpg');
    });

    it('should use placeholder when imageMode is none', async () => {
      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: { name: 'Test', description: 'Desc' },
      } as GenerationResult<TestEntity>);
      mockConfig.generateFn = mockGenerateFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      act(() => {
        result.current.setImageMode('none');
      });

      await act(async () => {
        await result.current.generate({});
      });

      expect(mockGenerateFn).toHaveBeenCalledWith({}, false);
      expect(result.current.image).toBe(mockConfig.placeholderImage);
    });

    it('should use placeholder when image generation fails', async () => {
      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: { name: 'Test', description: 'Desc' },
        // No image data
      } as GenerationResult<TestEntity>);
      mockConfig.generateFn = mockGenerateFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.generate({});
      });

      expect(result.current.image).toBe(mockConfig.placeholderImage);
    });

    it('should handle generation errors', async () => {
      const error = new Error('Generation failed');
      const mockGenerateFn = vi.fn().mockRejectedValue(error);
      const onError = vi.fn();
      mockConfig.generateFn = mockGenerateFn;
      mockConfig.onGenerationError = onError;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.generate({});
      });

      expect(result.current.isGenerating).toBe(false);
      expect(result.current.generatedData).toBeNull();
      expect(onError).toHaveBeenCalledWith(error);
    });

    it('should call onGenerationSuccess on success', async () => {
      const testData = { name: 'Test', description: 'Desc' };
      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: testData,
      } as GenerationResult<TestEntity>);
      const onSuccess = vi.fn();
      mockConfig.generateFn = mockGenerateFn;
      mockConfig.onGenerationSuccess = onSuccess;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.generate({});
      });

      expect(onSuccess).toHaveBeenCalledWith(testData);
    });

    it('should add logs during generation process', async () => {
      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: { name: 'Test', description: 'Desc' },
        imageUrl: 'https://example.com/img.jpg',
      } as GenerationResult<TestEntity>);
      mockConfig.generateFn = mockGenerateFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.generate({});
      });

      expect(result.current.logs).toContain('> COMMENCING GENERATION...');
      expect(result.current.logs).toContain('> FETCHING DATA...');
      expect(result.current.logs).toContain('> DATA RECEIVED.');
      expect(result.current.logs).toContain('> GENERATING VISUAL...');
      expect(result.current.logs).toContain('> VISUAL SYNTHESIS COMPLETE.');
      expect(result.current.logs).toContain('> GENERATION SUCCESSFUL.');
    });
  });

  describe('save', () => {
    it('should call saveFn with correct parameters', async () => {
      const mockSaveFn = vi.fn().mockResolvedValue(undefined);
      mockConfig.saveFn = mockSaveFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      let saveResult: boolean | undefined;
      await act(async () => {
        saveResult = await result.current.save('campaign-123', {
          name: 'Test Entity',
          description: 'Description',
          attributes: { stat: 10 },
          metadata: { source: 'generator' },
        });
      });

      expect(mockSaveFn).toHaveBeenCalledWith({
        name: 'Test Entity',
        description: 'Description',
        attributes: { stat: 10 },
        metadata: { source: 'generator' },
        imageUrl: undefined,
        generationRequestId: undefined,
      });
      expect(saveResult).toBe(true);
    });

    it('should return false when no campaignId provided', async () => {
      const mockSaveFn = vi.fn();
      mockConfig.saveFn = mockSaveFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      let saveResult: boolean | undefined;
      await act(async () => {
        saveResult = await result.current.save('', {
          name: 'Test',
          attributes: {},
        });
      });

      expect(mockSaveFn).not.toHaveBeenCalled();
      expect(saveResult).toBe(false);
    });

    it('should include imageUrl when image is not placeholder', async () => {
      const mockSaveFn = vi.fn().mockResolvedValue(undefined);
      mockConfig.saveFn = mockSaveFn;

      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: { name: 'Test', description: 'Desc' },
        imageUrl: 'https://example.com/generated.jpg',
        generationRequestId: 'req-456',
      } as GenerationResult<TestEntity>);
      mockConfig.generateFn = mockGenerateFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.generate({});
      });

      await act(async () => {
        await result.current.save('campaign-123', {
          name: 'Test',
          attributes: {},
        });
      });

      expect(mockSaveFn).toHaveBeenCalledWith(
        expect.objectContaining({
          imageUrl: 'https://example.com/generated.jpg',
          generationRequestId: 'req-456',
        })
      );
    });

    it('should handle save errors', async () => {
      const error = new Error('Save failed');
      const mockSaveFn = vi.fn().mockRejectedValue(error);
      const onError = vi.fn();
      mockConfig.saveFn = mockSaveFn;
      mockConfig.onSaveError = onError;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      let saveResult: boolean | undefined;
      await act(async () => {
        saveResult = await result.current.save('campaign-123', {
          name: 'Test',
          attributes: {},
        });
      });

      expect(saveResult).toBe(false);
      expect(onError).toHaveBeenCalledWith(error);
    });

    it('should call onSaveSuccess on successful save', async () => {
      const mockSaveFn = vi.fn().mockResolvedValue(undefined);
      const onSuccess = vi.fn();
      mockConfig.saveFn = mockSaveFn;
      mockConfig.onSaveSuccess = onSuccess;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.save('campaign-123', {
          name: 'Test',
          attributes: {},
        });
      });

      expect(onSuccess).toHaveBeenCalled();
    });

    it('should add logs during save process', async () => {
      const mockSaveFn = vi.fn().mockResolvedValue(undefined);
      mockConfig.saveFn = mockSaveFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.save('campaign-123', {
          name: 'Test',
          attributes: {},
        });
      });

      expect(result.current.logs).toContain('> WRITING TO STORAGE...');
      expect(result.current.logs).toContain('> SUCCESS: DATA COMMITTED.');
    });
  });

  describe('setEditableData', () => {
    it('should update editable data', () => {
      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      act(() => {
        result.current.setEditableData({ name: 'Edited', description: 'Edited desc' });
      });

      expect(result.current.editableData).toEqual({
        name: 'Edited',
        description: 'Edited desc',
      });
    });
  });

  describe('setImageMode', () => {
    it('should update image mode', () => {
      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      act(() => {
        result.current.setImageMode('upload');
      });

      expect(result.current.imageMode).toBe('upload');
    });
  });

  describe('setUploadedImageData', () => {
    it('should update uploaded image data', () => {
      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      act(() => {
        result.current.setUploadedImageData('newbased64data');
      });

      expect(result.current.uploadedImageData).toBe('newbased64data');
    });
  });

  describe('addLog', () => {
    it('should add a new log entry', () => {
      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      act(() => {
        result.current.addLog('New log message');
      });

      expect(result.current.logs).toContain('> New log message');
    });
  });

  describe('clearLogs', () => {
    it('should clear all logs', () => {
      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      act(() => {
        result.current.clearLogs();
      });

      expect(result.current.logs).toEqual([]);
    });
  });

  describe('reset', () => {
    it('should reset all state to initial values', async () => {
      const mockGenerateFn = vi.fn().mockResolvedValue({
        data: { name: 'Test', description: 'Desc' },
        imageUrl: 'https://example.com/img.jpg',
        generationRequestId: 'req-789',
      } as GenerationResult<TestEntity>);
      mockConfig.generateFn = mockGenerateFn;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.generate({});
      });

      act(() => {
        result.current.setEditableData({ name: 'Modified', description: 'Modified' });
        result.current.setImageMode('upload');
        result.current.setUploadedImageData('uploaddata');
      });

      act(() => {
        result.current.reset();
      });

      expect(result.current.generatedData).toBeNull();
      expect(result.current.editableData).toBeNull();
      expect(result.current.image).toBe(mockConfig.placeholderImage);
      expect(result.current.generationRequestId).toBeUndefined();
      expect(result.current.imageMode).toBe('generate');
      expect(result.current.uploadedImageData).toBeNull();
      expect(result.current.logs).toEqual([]);
    });
  });

  describe('logs max limit', () => {
    it('should respect maxLogs limit', () => {
      mockConfig.maxLogs = 3;
      mockConfig.initialLogs = [];

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      act(() => {
        result.current.addLog('Log 1');
        result.current.addLog('Log 2');
        result.current.addLog('Log 3');
        result.current.addLog('Log 4');
      });

      expect(result.current.logs.length).toBe(3);
      expect(result.current.logs).not.toContain('> Log 1');
      expect(result.current.logs).toContain('> Log 4');
    });
  });

  describe('loadFieldDefinitions', () => {
    it('should load field definitions when gameSystemId is provided', async () => {
      const mockFields = [
        { identifier: 'strength', displayName: 'Strength', fieldType: 'number' },
        { identifier: 'dexterity', displayName: 'Dexterity', fieldType: 'number' },
      ];
      const mockGetFieldDefinitions = vi.fn().mockResolvedValue(mockFields);
      mockConfig.getFieldDefinitions = mockGetFieldDefinitions;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.loadFieldDefinitions('game-system-123');
      });

      expect(mockGetFieldDefinitions).toHaveBeenCalledWith('game-system-123');
      expect(result.current.fieldDefinitions).toEqual(mockFields);
    });

    it('should clear field definitions when gameSystemId is undefined', async () => {
      const mockGetFieldDefinitions = vi.fn();
      mockConfig.getFieldDefinitions = mockGetFieldDefinitions;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.loadFieldDefinitions(undefined);
      });

      expect(mockGetFieldDefinitions).not.toHaveBeenCalled();
      expect(result.current.fieldDefinitions).toEqual([]);
    });

    it('should handle errors when loading field definitions', async () => {
      const mockGetFieldDefinitions = vi.fn().mockRejectedValue(new Error('Failed to load'));
      mockConfig.getFieldDefinitions = mockGetFieldDefinitions;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.loadFieldDefinitions('game-system-123');
      });

      expect(result.current.fieldDefinitions).toEqual([]);
    });

    it('should do nothing when getFieldDefinitions is not provided', async () => {
      mockConfig.getFieldDefinitions = undefined;

      const { result } = renderHook(() => useEntityGeneration(mockConfig));

      await act(async () => {
        await result.current.loadFieldDefinitions('game-system-123');
      });

      expect(result.current.fieldDefinitions).toEqual([]);
    });
  });
});
