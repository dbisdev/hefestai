/**
 * API Types and Response interfaces
 * Single Responsibility: Only API-related type definitions
 */

export interface ApiError {
  message: string;
  code?: string;
  errors?: Record<string, string[]>;
  status?: number;
}

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

// AI Generation Request/Response types
export interface CharacterGenerationParams {
  species: string;
  role: string;
  morphology: string;
  attire: string;
}

export interface CharacterGenerationResponse {
  success: boolean;
  characterJson?: string;
  imageBase64?: string;
  imageUrl?: string;
  error?: string;
}

export interface SolarSystemGenerationParams {
  spectralClass: string;
  planetCount: number;
}

export interface SolarSystemGenerationResponse {
  success: boolean;
  systemJson?: string;
  imageBase64?: string;
  imageUrl?: string;
  error?: string;
}

export interface VehicleGenerationParams {
  type: string;
  class: string;
  engine: string;
}

export interface VehicleGenerationResponse {
  success: boolean;
  vehicleJson?: string;
  error?: string;
}

// HTTP client types
export interface RequestConfig extends RequestInit {
  skipAuth?: boolean;
  timeout?: number;
}

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
