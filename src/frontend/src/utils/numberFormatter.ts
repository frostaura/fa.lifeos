/**
 * Centralized number formatting utility for consistent formatting across the application.
 * Format: "R -1 838 297.71" with spaces as thousand separators
 */

/**
 * Formats a number with spaces as thousand separators
 * @param value - The number to format
 * @param decimals - Number of decimal places (default: 2)
 * @returns Formatted number string with spaces (e.g., "1 838 297.71")
 */
export function formatNumberWithSpaces(value: number, decimals: number = 2): string {
  const fixed = value.toFixed(decimals);
  const [integerPart, decimalPart] = fixed.split('.');
  
  // Add spaces as thousand separators
  const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
  
  return decimalPart ? `${formattedInteger}.${decimalPart}` : formattedInteger;
}

/**
 * Formats a currency amount in South African Rand format
 * @param value - The amount to format
 * @param decimals - Number of decimal places (default: 2)
 * @returns Formatted currency string (e.g., "R -1 838 297.71")
 */
export function formatCurrency(value: number | null | undefined, decimals: number = 2): string {
  if (value === null || value === undefined) {
    return 'R 0';
  }
  
  const isNegative = value < 0;
  const absoluteValue = Math.abs(value);
  const formattedNumber = formatNumberWithSpaces(absoluteValue, decimals);
  
  return isNegative ? `R -${formattedNumber}` : `R ${formattedNumber}`;
}

/**
 * Formats a currency amount without decimals (for whole numbers)
 * @param value - The amount to format
 * @returns Formatted currency string without decimals (e.g., "R 1 838 297")
 */
export function formatCurrencyWhole(value: number | null | undefined): string {
  return formatCurrency(value, 0);
}

/**
 * Formats a percentage value
 * @param value - The percentage to format (e.g., 0.15 for 15%)
 * @param decimals - Number of decimal places (default: 1)
 * @returns Formatted percentage string (e.g., "15.0%")
 */
export function formatPercentage(value: number | null | undefined, decimals: number = 1): string {
  if (value === null || value === undefined) {
    return '0%';
  }
  
  return `${formatNumberWithSpaces(value, decimals)}%`;
}

/**
 * Formats a plain number with thousand separators
 * @param value - The number to format
 * @param decimals - Number of decimal places (default: 0)
 * @returns Formatted number string (e.g., "1 838 297")
 */
export function formatNumber(value: number | null | undefined, decimals: number = 0): string {
  if (value === null || value === undefined) {
    return '0';
  }
  
  return formatNumberWithSpaces(value, decimals);
}
