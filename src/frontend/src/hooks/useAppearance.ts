import { useEffect } from 'react';
import { useGetProfileQuery } from '@/services/endpoints/settings';

export function useAppearance() {
  // Only enable query if user is authenticated
  const token = localStorage.getItem('accessToken');
  const { data: profile } = useGetProfileQuery(undefined, {
    skip: !token, // Skip query if no token
  });

  useEffect(() => {
    if (!profile?.appearance) return;

    const { accentColor, baseFontSize, themeMode, orbColor1, orbColor2, orbColor3 } = profile.appearance;
    const root = document.documentElement;

    // Apply accent color
    if (accentColor) {
      root.style.setProperty('--accent-color', accentColor);
    }

    // Apply base font size
    if (baseFontSize) {
      root.style.setProperty('--base-font-size', `${baseFontSize}rem`);
    }

    // Apply theme mode
    if (themeMode) {
      applyThemeMode(themeMode);
    }

    // Apply orb colors for gradient text
    if (orbColor1 && orbColor2 && orbColor3) {
      root.style.setProperty('--orb-color-1', orbColor1);
      root.style.setProperty('--orb-color-2', orbColor2);
      root.style.setProperty('--orb-color-3', orbColor3);
    }
  }, [profile]);
}

function applyThemeMode(mode: string) {
  const root = document.documentElement;
  
  // Remove both classes first
  root.classList.remove('dark', 'light');
  
  if (mode === 'system') {
    // Check system preference
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    root.classList.add(prefersDark ? 'dark' : 'light');
  } else {
    root.classList.add(mode);
  }
}

// Listen for system theme changes
if (typeof window !== 'undefined') {
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    const root = document.documentElement;
    if (root.classList.contains('system')) {
      root.classList.toggle('dark', e.matches);
      root.classList.toggle('light', !e.matches);
    }
  });
}
