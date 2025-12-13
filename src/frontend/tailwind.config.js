/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Accent colors for gradients
        accent: {
          purple: 'var(--accent-color, #8b5cf6)',
          cyan: '#22d3ee',
          pink: '#ec4899',
        },
        // Background gradients (now using CSS variables)
        bg: {
          primary: 'var(--bg-primary)',
          secondary: 'var(--bg-secondary)',
          tertiary: 'var(--bg-tertiary)',
        },
        // Glass effect backgrounds
        glass: {
          light: 'rgba(255, 255, 255, 0.05)',
          medium: 'rgba(255, 255, 255, 0.08)',
          heavy: 'rgba(255, 255, 255, 0.12)',
          border: 'var(--border-glass)',
        },
        // Background hover
        background: {
          primary: 'var(--bg-primary)',
          secondary: 'var(--bg-secondary)',
          tertiary: 'var(--bg-tertiary)',
          hover: 'var(--bg-hover)',
        },
        // Neon accent colors
        neon: {
          cyan: '#00d4ff',
          purple: '#a855f7',
          pink: '#ec4899',
          green: '#22c55e',
          orange: '#f97316',
          yellow: '#eab308',
        },
        // Text colors (now using CSS variables)
        text: {
          primary: 'var(--text-primary)',
          secondary: 'var(--text-secondary)',
          muted: 'var(--text-tertiary)',
          disabled: 'rgba(var(--text-secondary), 0.5)',
        },
        // Semantic colors
        semantic: {
          success: '#22c55e',
          warning: '#f59e0b',
          error: '#ef4444',
          info: '#3b82f6',
        },
        // Dimension colors
        dimension: {
          health: '#22c55e',
          wealth: '#eab308',
          relationships: '#ec4899',
          career: '#3b82f6',
          growth: '#a855f7',
          fun: '#f97316',
          environment: '#14b8a6',
          spirituality: '#8b5cf6',
        },
      },
      boxShadow: {
        'glow-sm': '0 0 20px rgba(168, 85, 247, 0.2)',
        'elevated': '0 10px 25px rgba(0, 0, 0, 0.5)',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
      },
      fontSize: {
        xs: ['0.75rem', { lineHeight: '1rem' }],
        sm: ['0.875rem', { lineHeight: '1.25rem' }],
        base: ['1rem', { lineHeight: '1.5rem' }],
        lg: ['1.125rem', { lineHeight: '1.75rem' }],
        xl: ['1.25rem', { lineHeight: '1.75rem' }],
        '2xl': ['1.5rem', { lineHeight: '2rem' }],
        '3xl': ['1.875rem', { lineHeight: '2.25rem' }],
        '4xl': ['2.25rem', { lineHeight: '2.5rem' }],
        '5xl': ['3rem', { lineHeight: '1' }],
      },
      spacing: {
        px: '1px',
        0: '0',
        0.5: '0.125rem',
        1: '0.25rem',
        2: '0.5rem',
        3: '0.75rem',
        4: '1rem',
        5: '1.25rem',
        6: '1.5rem',
        8: '2rem',
        10: '2.5rem',
        12: '3rem',
        16: '4rem',
        20: '5rem',
        24: '6rem',
      },
      screens: {
        sm: '320px',
        md: '768px',
        lg: '1024px',
        xl: '1440px',
        '2xl': '1920px',
      },
      borderRadius: {
        none: '0',
        sm: '0.25rem',
        DEFAULT: '0.5rem',
        md: '0.75rem',
        lg: '1rem',
        xl: '1.5rem',
        '2xl': '2rem',
        full: '9999px',
      },
      boxShadow: {
        sm: '0 1px 2px rgba(0, 0, 0, 0.5)',
        DEFAULT: '0 4px 6px rgba(0, 0, 0, 0.5)',
        md: '0 6px 12px rgba(0, 0, 0, 0.5)',
        lg: '0 10px 25px rgba(0, 0, 0, 0.5)',
        xl: '0 20px 40px rgba(0, 0, 0, 0.5)',
        'glow-cyan': '0 0 20px rgba(0, 212, 255, 0.3)',
        'glow-purple': '0 0 20px rgba(168, 85, 247, 0.3)',
        'glow-pink': '0 0 20px rgba(236, 72, 153, 0.3)',
      },
      backdropBlur: {
        xs: '2px',
        sm: '4px',
        DEFAULT: '8px',
        md: '12px',
        lg: '16px',
        xl: '24px',
      },
      animation: {
        'fade-in': 'fadeIn 0.2s ease-in-out',
        'slide-up': 'slideUp 0.3s ease-out',
        'pulse-glow': 'pulseGlow 2s infinite',
        'gradient-shift': 'gradientShift 20s ease-in-out infinite',
        'gradient-shift-reverse': 'gradientShiftReverse 25s ease-in-out infinite',
        'gradient-shift-diagonal': 'gradientShiftDiagonal 30s ease-in-out infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { opacity: '0', transform: 'translateY(10px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        pulseGlow: {
          '0%, 100%': { boxShadow: '0 0 20px rgba(168, 85, 247, 0.3)' },
          '50%': { boxShadow: '0 0 40px rgba(168, 85, 247, 0.5)' },
        },
        gradientShift: {
          '0%': { transform: 'translate(-50%, -50%) scale(1)', opacity: '1' },
          '25%': { transform: 'translate(20%, 0%) scale(1.1)', opacity: '0.2' },
          '50%': { transform: 'translate(80%, 60%) scale(1.2)', opacity: '1' },
          '75%': { transform: 'translate(30%, 80%) scale(1.1)', opacity: '0.15' },
          '100%': { transform: 'translate(-50%, -50%) scale(1)', opacity: '1' },
        },
        gradientShiftReverse: {
          '0%': { transform: 'translate(50%, 50%) scale(1)', opacity: '1' },
          '25%': { transform: 'translate(-20%, 80%) scale(1.15)', opacity: '0.15' },
          '50%': { transform: 'translate(-80%, 20%) scale(1.25)', opacity: '1' },
          '75%': { transform: 'translate(0%, -30%) scale(1.1)', opacity: '0.2' },
          '100%': { transform: 'translate(50%, 50%) scale(1)', opacity: '1' },
        },
        gradientShiftDiagonal: {
          '0%': { transform: 'translate(30%, -40%) scale(1)', opacity: '1' },
          '33%': { transform: 'translate(-60%, 30%) scale(1.2)', opacity: '0.15' },
          '66%': { transform: 'translate(50%, 90%) scale(1.15)', opacity: '1' },
          '100%': { transform: 'translate(30%, -40%) scale(1)', opacity: '1' },
        },
      },
    },
  },
  plugins: [],
}
