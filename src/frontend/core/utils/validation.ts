/**
 * Input Validation Utilities
 * OWASP A03: Injection Prevention through strict input validation
 * Single Responsibility: Only input validation logic
 */

import { MAX_INPUT_LENGTH } from '../config/constants';

export interface ValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Validates email format (OWASP compliant)
 * - Checks format and length
 * - Prevents header injection characters
 */
export function validateEmail(email: string): ValidationResult {
  const errors: string[] = [];
  
  if (!email) {
    errors.push('El correo electrónico es requerido');
    return { isValid: false, errors };
  }

  if (email.length > MAX_INPUT_LENGTH.email) {
    errors.push(`El correo no puede exceder ${MAX_INPUT_LENGTH.email} caracteres`);
  }

  // RFC 5322 compliant email regex (simplified)
  const emailRegex = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;
  
  if (!emailRegex.test(email)) {
    errors.push('Formato de correo electrónico inválido');
  }

  // Check for potential header injection
  if (/[\r\n]/.test(email)) {
    errors.push('El correo contiene caracteres inválidos');
  }

  return { isValid: errors.length === 0, errors };
}

/**
 * Validates password strength (OWASP compliant)
 * - Minimum length check
 * - Character variety requirements
 */
export function validatePassword(password: string): ValidationResult {
  const errors: string[] = [];

  if (!password) {
    errors.push('La contraseña es requerida');
    return { isValid: false, errors };
  }

  if (password.length < 8) {
    errors.push('La contraseña debe tener al menos 8 caracteres');
  }

  if (password.length > MAX_INPUT_LENGTH.password) {
    errors.push(`La contraseña no puede exceder ${MAX_INPUT_LENGTH.password} caracteres`);
  }

  // Check for character variety
  if (!/[a-z]/.test(password)) {
    errors.push('La contraseña debe contener al menos una letra minúscula');
  }

  if (!/[A-Z]/.test(password)) {
    errors.push('La contraseña debe contener al menos una letra mayúscula');
  }

  if (!/[0-9]/.test(password)) {
    errors.push('La contraseña debe contener al menos un número');
  }

  return { isValid: errors.length === 0, errors };
}

/**
 * Validates display name
 * - Length check
 * - Allowed characters check
 */
export function validateDisplayName(name: string): ValidationResult {
  const errors: string[] = [];

  if (!name) {
    errors.push('El nombre de usuario es requerido');
    return { isValid: false, errors };
  }

  if (name.length < 2) {
    errors.push('El nombre debe tener al menos 2 caracteres');
  }

  if (name.length > MAX_INPUT_LENGTH.displayName) {
    errors.push(`El nombre no puede exceder ${MAX_INPUT_LENGTH.displayName} caracteres`);
  }

  // Allow alphanumeric, underscores, and spaces
  if (!/^[a-zA-Z0-9_\s]+$/.test(name)) {
    errors.push('El nombre solo puede contener letras, números, guiones bajos y espacios');
  }

  return { isValid: errors.length === 0, errors };
}

/**
 * Validates invitation code format
 */
export function validateInviteCode(code: string): ValidationResult {
  const errors: string[] = [];

  if (!code) {
    errors.push('El código de invitación es requerido');
    return { isValid: false, errors };
  }

  if (code.length > MAX_INPUT_LENGTH.inviteCode) {
    errors.push(`El código no puede exceder ${MAX_INPUT_LENGTH.inviteCode} caracteres`);
  }

  // Alphanumeric with hyphens
  if (!/^[A-Z0-9-]+$/i.test(code)) {
    errors.push('Formato de código inválido');
  }

  return { isValid: errors.length === 0, errors };
}

/**
 * Generic text input validation
 * - Prevents XSS through input validation
 * - Length validation
 */
export function validateTextInput(
  value: string,
  fieldName: string,
  maxLength: number,
  required = true
): ValidationResult {
  const errors: string[] = [];

  if (required && !value) {
    errors.push(`${fieldName} es requerido`);
    return { isValid: false, errors };
  }

  if (value && value.length > maxLength) {
    errors.push(`${fieldName} no puede exceder ${maxLength} caracteres`);
  }

  // Check for potentially dangerous patterns (basic XSS prevention at input)
  const dangerousPatterns = [
    /<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi,
    /javascript:/gi,
    /on\w+\s*=/gi,
  ];

  for (const pattern of dangerousPatterns) {
    if (pattern.test(value)) {
      errors.push(`${fieldName} contiene caracteres no permitidos`);
      break;
    }
  }

  return { isValid: errors.length === 0, errors };
}

/**
 * Validates entity data for saving
 */
export function validateEntityInput(entity: {
  name: string;
  description?: string;
}): ValidationResult {
  const nameValidation = validateTextInput(entity.name, 'Nombre', MAX_INPUT_LENGTH.entityName);
  
  const descValidation = entity.description 
    ? validateTextInput(entity.description, 'Descripción', MAX_INPUT_LENGTH.description, false)
    : { isValid: true, errors: [] };

  return {
    isValid: nameValidation.isValid && descValidation.isValid,
    errors: [...nameValidation.errors, ...descValidation.errors],
  };
}
