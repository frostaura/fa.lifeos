import { useState, useEffect } from 'react';

export type ChartPeriod = '1M' | '3M' | '6M' | '1Y' | '5Y' | '10Y' | 'ALL';

const STORAGE_KEY = 'lifeos-chart-period';
const DEFAULT_PERIOD: ChartPeriod = '5Y';

/**
 * Custom hook to manage shared chart period state across components
 * Persists to localStorage and syncs across tabs/pages
 */
export function useChartPeriod() {
  const [period, setPeriodState] = useState<ChartPeriod>(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored && ['1M', '3M', '6M', '1Y', '5Y', '10Y', 'ALL'].includes(stored)) {
        return stored as ChartPeriod;
      }
    } catch (err) {
      console.error('Failed to read chart period from localStorage:', err);
    }
    return DEFAULT_PERIOD;
  });

  const setPeriod = (newPeriod: ChartPeriod) => {
    try {
      localStorage.setItem(STORAGE_KEY, newPeriod);
      setPeriodState(newPeriod);
    } catch (err) {
      console.error('Failed to save chart period to localStorage:', err);
      setPeriodState(newPeriod); // Still update state even if localStorage fails
    }
  };

  // Listen for changes from other tabs/components
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY && e.newValue) {
        setPeriodState(e.newValue as ChartPeriod);
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, []);

  return [period, setPeriod] as const;
}

/**
 * Get the number of months for a given period
 */
export function getMonthsForPeriod(period: ChartPeriod): number {
  const monthsMap: Record<ChartPeriod, number> = {
    '1M': 1,
    '3M': 3,
    '6M': 6,
    '1Y': 12,
    '5Y': 60,
    '10Y': 120,
    'ALL': 240, // 20 years for "ALL"
  };
  return monthsMap[period];
}
