/** Format integers with Western-style comma grouping (e.g. 1,265,687). */
export function formatNumber(
  value: number | string | null | undefined,
): string {
  if (value == null || value === '') return '';
  const num = typeof value === 'string' ? Number(value) : value;
  if (!Number.isFinite(num)) return String(value);
  return num.toLocaleString('en-US');
}
