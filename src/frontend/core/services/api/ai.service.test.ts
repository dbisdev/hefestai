/**
 * AI Generation Service Unit Tests
 * Tests for AI content generation operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { aiService } from './ai.service';
import { httpClient } from './client';

vi.mock('./client', () => ({
  httpClient: {
    post: vi.fn(),
  },
}));

describe('AI Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('generateCharacter', () => {
    it('generates a character successfully', async () => {
      const mockResponse = {
        success: true,
        characterJson: '{"name":"Test Character"}',
        imageBase64: 'base64data',
        imageUrl: 'https://example.com/image.png',
        generationRequestId: 'gen-123',
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResponse);
      
      const result = await aiService.generateCharacter({
        gameSystemId: 'system-1',
        species: 'human',
        role: 'operative',
        morphology: 'NEUTRAL',
        attire: 'Techwear',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/ai/generate/character', {
        gameSystemId: 'system-1',
        species: 'human',
        role: 'operative',
        morphology: 'NEUTRAL',
        attire: 'Techwear',
      });
      expect(result).toEqual({
        characterJson: '{"name":"Test Character"}',
        imageBase64: 'base64data',
        imageUrl: 'https://example.com/image.png',
        generationRequestId: 'gen-123',
      });
    });

    it('throws on failed generation', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        success: false,
        error: 'Generation failed',
      });
      
      await expect(aiService.generateCharacter({
        species: 'human',
        role: 'operative',
        morphology: 'NEUTRAL',
        attire: 'Techwear',
      })).rejects.toThrow('Generation failed');
    });

    it('uses default error message', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        success: false,
      });
      
      await expect(aiService.generateCharacter({
        species: 'human',
        role: 'operative',
        morphology: 'NEUTRAL',
        attire: 'Techwear',
      })).rejects.toThrow('Character generation failed');
    });
  });

  describe('generateSolarSystem', () => {
    it('generates a solar system successfully', async () => {
      const mockResponse = {
        success: true,
        systemJson: '{"name":"Test System"}',
        generationRequestId: 'gen-456',
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResponse);
      
      const result = await aiService.generateSolarSystem({
        gameSystemId: 'system-1',
        spectralClass: 'G',
        planetCount: 5,
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/ai/generate/solar-system', {
        gameSystemId: 'system-1',
        spectralClass: 'G',
        planetCount: 5,
      });
      expect(result.systemJson).toBe('{"name":"Test System"}');
      expect(result.generationRequestId).toBe('gen-456');
    });

    it('throws on failed generation', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        success: false,
        error: 'System creation failed',
      });
      
      await expect(aiService.generateSolarSystem({
        spectralClass: 'G',
        planetCount: 3,
      })).rejects.toThrow('System creation failed');
    });
  });

  describe('generateVehicle', () => {
    it('generates a vehicle successfully', async () => {
      const mockResponse = {
        success: true,
        vehicleJson: '{"name":"Test Vehicle"}',
        generationRequestId: 'gen-789',
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResponse);
      
      const result = await aiService.generateVehicle({
        gameSystemId: 'system-1',
        type: 'starship',
        class: 'interceptor',
        engine: 'fusion',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/ai/generate/vehicle', {
        gameSystemId: 'system-1',
        type: 'starship',
        class: 'interceptor',
        engine: 'fusion',
      });
      expect(result.vehicleJson).toBe('{"name":"Test Vehicle"}');
    });

    it('throws on failed generation', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        success: false,
      });
      
      await expect(aiService.generateVehicle({
        type: 'starship',
        class: 'interceptor',
        engine: 'fusion',
      })).rejects.toThrow('Vehicle generation failed');
    });
  });

  describe('generateNpc', () => {
    it('generates an NPC successfully', async () => {
      const mockResponse = {
        success: true,
        npcJson: '{"name":"Test NPC"}',
        generationRequestId: 'gen-npc',
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResponse);
      
      const result = await aiService.generateNpc({
        gameSystemId: 'system-1',
        species: 'human',
        occupation: 'merchant',
        personality: 'friendly',
        setting: 'space station',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/ai/generate/npc', {
        gameSystemId: 'system-1',
        species: 'human',
        occupation: 'merchant',
        personality: 'friendly',
        setting: 'space station',
      });
      expect(result.npcJson).toBe('{"name":"Test NPC"}');
    });

    it('throws on failed generation', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        success: false,
      });
      
      await expect(aiService.generateNpc({
        species: 'human',
        occupation: 'merchant',
        personality: 'friendly',
        setting: 'space station',
      })).rejects.toThrow('NPC generation failed');
    });
  });

  describe('generateEnemy', () => {
    it('generates an enemy successfully', async () => {
      const mockResponse = {
        success: true,
        enemyJson: '{"name":"Test Enemy"}',
        generationRequestId: 'gen-enemy',
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResponse);
      
      const result = await aiService.generateEnemy({
        gameSystemId: 'system-1',
        species: 'xenomorph',
        threatLevel: 'high',
        behavior: 'aggressive',
        environment: 'spaceship',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/ai/generate/enemy', {
        gameSystemId: 'system-1',
        species: 'xenomorph',
        threatLevel: 'high',
        behavior: 'aggressive',
        environment: 'spaceship',
      });
      expect(result.enemyJson).toBe('{"name":"Test Enemy"}');
    });

    it('throws on failed generation', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        success: false,
      });
      
      await expect(aiService.generateEnemy({
        species: 'xenomorph',
        threatLevel: 'high',
        behavior: 'aggressive',
        environment: 'spaceship',
      })).rejects.toThrow('Enemy generation failed');
    });
  });

  describe('generateMission', () => {
    it('generates a mission successfully', async () => {
      const mockResponse = {
        success: true,
        missionJson: '{"name":"Test Mission"}',
        generationRequestId: 'gen-mission',
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResponse);
      
      const result = await aiService.generateMission({
        gameSystemId: 'system-1',
        missionType: 'escort',
        difficulty: 'hard',
        environment: 'planet',
        factionInvolved: 'corporation',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/ai/generate/mission', {
        gameSystemId: 'system-1',
        missionType: 'escort',
        difficulty: 'hard',
        environment: 'planet',
        factionInvolved: 'corporation',
      });
      expect(result.missionJson).toBe('{"name":"Test Mission"}');
    });

    it('throws on failed generation', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        success: false,
      });
      
      await expect(aiService.generateMission({
        missionType: 'escort',
        difficulty: 'hard',
        environment: 'planet',
        factionInvolved: 'corporation',
      })).rejects.toThrow('Mission generation failed');
    });
  });

  describe('generateEncounter', () => {
    it('generates an encounter successfully', async () => {
      const mockResponse = {
        success: true,
        encounterJson: '{"name":"Test Encounter"}',
        generationRequestId: 'gen-encounter',
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResponse);
      
      const result = await aiService.generateEncounter({
        gameSystemId: 'system-1',
        encounterType: 'combat',
        difficulty: 'medium',
        environment: 'asteroid',
        enemyCount: '3',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/ai/generate/encounter', {
        gameSystemId: 'system-1',
        encounterType: 'combat',
        difficulty: 'medium',
        environment: 'asteroid',
        enemyCount: '3',
      });
      expect(result.encounterJson).toBe('{"name":"Test Encounter"}');
    });

    it('throws on failed generation', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        success: false,
      });
      
      await expect(aiService.generateEncounter({
        encounterType: 'combat',
        difficulty: 'medium',
        environment: 'asteroid',
        enemyCount: '3',
      })).rejects.toThrow('Encounter generation failed');
    });
  });
});
