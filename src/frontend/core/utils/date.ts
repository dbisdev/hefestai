/**
 * Date utilities
 * Shared date-formatting helpers used across the application.
 */

/**
 * Formats a nullable ISO date string for display in admin panels.
 *
 * @param dateStr  - ISO date string or null/undefined
 * @param showTime - When true, includes HH:MM (default: false)
 * @param nullLabel - Text to return when dateStr is falsy (default: 'N/A')
 * @returns Localised date string in es-ES locale
 *
 * @example
 * formatAdminDate('2024-01-15T10:30:00Z', true)  // '15/01/2024, 10:30'
 * formatAdminDate(null)                           // 'N/A'
 * formatAdminDate(null, false, 'Nunca')           // 'Nunca'
 */
export function formatAdminDate(
  dateStr: string | null | undefined,
  showTime = false,
  nullLabel = 'N/A',
): string {
  if (!dateStr) return nullLabel;

  const options: Intl.DateTimeFormatOptions = {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    ...(showTime ? { hour: '2-digit', minute: '2-digit' } : {}),
  };

  return new Date(dateStr).toLocaleDateString('es-ES', options);
}
