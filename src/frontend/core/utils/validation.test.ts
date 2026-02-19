/**
 * Validation Utilities Tests
 * Tests for input validation functions
 */
import { describe, it, expect } from 'vitest';
import {
  validateEmail,
  validatePassword,
  validateDisplayName,
  validateInviteCode,
  validateTextInput,
  validateEntityInput,
} from './validation';

describe('Validation Utilities', () => {
  describe('validateEmail', () => {
    it('validates a correct email', () => {
      const result = validateEmail('test@example.com');
      expect(result.isValid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('rejects empty email', () => {
      const result = validateEmail('');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('El correo electrónico es requerido');
    });

    it('rejects invalid email format', () => {
      const result = validateEmail('invalid-email');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('Formato de correo electrónico inválido');
    });

    it('rejects email with header injection characters', () => {
      const result = validateEmail('test@example.com\r\nBcc:attacker@evil.com');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('El correo contiene caracteres inválidos');
    });

    it('rejects email that is too long', () => {
      const longEmail = 'a'.repeat(250) + '@example.com';
      const result = validateEmail(longEmail);
      expect(result.isValid).toBe(false);
    });
  });

  describe('validatePassword', () => {
    it('validates a strong password', () => {
      const result = validatePassword('Password123');
      expect(result.isValid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('rejects empty password', () => {
      const result = validatePassword('');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('La contraseña es requerida');
    });

    it('rejects password shorter than 8 characters', () => {
      const result = validatePassword('Pass1');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('La contraseña debe tener al menos 8 caracteres');
    });

    it('rejects password without lowercase letter', () => {
      const result = validatePassword('PASSWORD123');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('La contraseña debe contener al menos una letra minúscula');
    });

    it('rejects password without uppercase letter', () => {
      const result = validatePassword('password123');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('La contraseña debe contener al menos una letra mayúscula');
    });

    it('rejects password without number', () => {
      const result = validatePassword('Password');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('La contraseña debe contener al menos un número');
    });

    it('collects multiple errors', () => {
      const result = validatePassword('pass');
      expect(result.isValid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(1);
    });
  });

  describe('validateDisplayName', () => {
    it('validates a correct display name', () => {
      const result = validateDisplayName('John Doe');
      expect(result.isValid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('rejects empty name', () => {
      const result = validateDisplayName('');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('El nombre de usuario es requerido');
    });

    it('rejects name shorter than 2 characters', () => {
      const result = validateDisplayName('A');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('El nombre debe tener al menos 2 caracteres');
    });

    it('rejects name with invalid characters', () => {
      const result = validateDisplayName('John@Doe');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('El nombre solo puede contener letras, números, guiones bajos y espacios');
    });

    it('accepts underscores and spaces', () => {
      const result = validateDisplayName('John_Doe 123');
      expect(result.isValid).toBe(true);
    });
  });

  describe('validateInviteCode', () => {
    it('validates a correct invite code', () => {
      const result = validateInviteCode('ABC123-XYZ');
      expect(result.isValid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('rejects empty code', () => {
      const result = validateInviteCode('');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('El código de invitación es requerido');
    });

    it('rejects code with invalid characters', () => {
      const result = validateInviteCode('ABC@123');
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('Formato de código inválido');
    });

    it('accepts alphanumeric and hyphens', () => {
      const result = validateInviteCode('A1B2-C3D4');
      expect(result.isValid).toBe(true);
    });
  });

  describe('validateTextInput', () => {
    it('validates required text input', () => {
      const result = validateTextInput('Valid input', 'Field', 100);
      expect(result.isValid).toBe(true);
    });

    it('rejects empty required field', () => {
      const result = validateTextInput('', 'Field', 100, true);
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('Field es requerido');
    });

    it('accepts empty optional field', () => {
      const result = validateTextInput('', 'Field', 100, false);
      expect(result.isValid).toBe(true);
    });

    it('rejects text that is too long', () => {
      const result = validateTextInput('a'.repeat(150), 'Field', 100);
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('Field no puede exceder 100 caracteres');
    });

    it('rejects text with script tags', () => {
      const result = validateTextInput('<script>alert(1)</script>', 'Field', 100);
      expect(result.isValid).toBe(false);
      expect(result.errors).toContain('Field contiene caracteres no permitidos');
    });

    it('rejects text with javascript: protocol', () => {
      const result = validateTextInput('javascript:alert(1)', 'Field', 100);
      expect(result.isValid).toBe(false);
    });

    it('rejects text with event handlers', () => {
      const result = validateTextInput('onclick=alert(1)', 'Field', 100);
      expect(result.isValid).toBe(false);
    });
  });

  describe('validateEntityInput', () => {
    it('validates a correct entity input', () => {
      const result = validateEntityInput({
        name: 'Test Entity',
        description: 'A test description',
      });
      expect(result.isValid).toBe(true);
    });

    it('rejects entity without name', () => {
      const result = validateEntityInput({
        name: '',
        description: 'A test description',
      });
      expect(result.isValid).toBe(false);
    });

    it('validates entity without optional description', () => {
      const result = validateEntityInput({
        name: 'Test Entity',
      });
      expect(result.isValid).toBe(true);
    });

    it('collects errors from both name and description', () => {
      const result = validateEntityInput({
        name: '',
        description: '<script>alert(1)</script>',
      });
      expect(result.isValid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(1);
    });
  });
});
