/**
 * Role Utilities
 * Single Responsibility: Centralize all conversions between the two role
 * representations used in this application:
 *
 *  - UserRole  (string union): 'MASTER' | 'PLAYER' | 'ADMIN'
 *    Used internally by the frontend (AuthContext, guards, UI checks).
 *
 *  - AdminUserRole (numeric enum): Player=0 | Master=1 | Admin=2
 *    Used only when communicating with the admin REST endpoints.
 *
 * Keeping the conversions here avoids scattering `.toUpperCase()` calls,
 * magic strings, and switch-statements throughout the codebase (DRY).
 */

import type { UserRole } from '@core/types';
import { AdminUserRole } from '@core/types';

// ── Spanish display labels for UserRole ──────────────────────────────────────

/** Spanish display labels for the string `UserRole`. */
export const USER_ROLE_LABELS: Record<UserRole, string> = {
  ADMIN: 'ADMINISTRADOR',
  MASTER: 'MAESTRO DE JUEGO',
  PLAYER: 'JUGADOR',
};

/**
 * Returns the Spanish display label for a given `UserRole`.
 * Falls back to `'USUARIO'` for unknown values (defensive).
 *
 * @param role - The frontend `UserRole` string.
 * @returns Human-readable Spanish label.
 */
export function userRoleToLabel(role: UserRole | null | undefined): string {
  if (!role) return 'INVITADO';
  return USER_ROLE_LABELS[role] ?? 'USUARIO';
}

// ── API format conversions ────────────────────────────────────────────────────

/**
 * Converts a `UserRole` to the PascalCase format expected by the backend
 * registration endpoint (e.g. `'MASTER'` → `'Master'`).
 *
 * @param role - The frontend `UserRole` string.
 * @returns Backend-compatible PascalCase role string.
 */
export function toApiRoleFormat(role: UserRole): string {
  return role.charAt(0) + role.slice(1).toLowerCase();
}

/**
 * Converts a raw role string from an API response to a `UserRole`.
 * The backend may return `'Master'`, `'master'`, or `'MASTER'` — all map
 * to `'MASTER'`.
 *
 * @param role - Role string from the API response.
 * @returns Normalised `UserRole`.
 */
export function fromApiRoleFormat(role: string): UserRole {
  return role.toUpperCase() as UserRole;
}

// ── Numeric ↔ string conversions ──────────────────────────────────────────────

/**
 * Maps a string `UserRole` to the numeric `AdminUserRole` enum used by
 * admin API endpoints.
 *
 * @param role - The frontend `UserRole` string.
 * @returns Corresponding `AdminUserRole` value.
 */
export function userRoleToAdminEnum(role: UserRole): AdminUserRole {
  switch (role) {
    case 'ADMIN':  return AdminUserRole.Admin;
    case 'MASTER': return AdminUserRole.Master;
    case 'PLAYER':
    default:       return AdminUserRole.Player;
  }
}

/**
 * Maps a numeric `AdminUserRole` enum value back to a string `UserRole`.
 *
 * @param role - Numeric `AdminUserRole` value from the admin API.
 * @returns Corresponding `UserRole` string.
 */
export function adminEnumToUserRole(role: AdminUserRole): UserRole {
  switch (role) {
    case AdminUserRole.Admin:  return 'ADMIN';
    case AdminUserRole.Master: return 'MASTER';
    case AdminUserRole.Player:
    default:                   return 'PLAYER';
  }
}
