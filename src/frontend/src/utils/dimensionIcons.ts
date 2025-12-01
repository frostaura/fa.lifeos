import {
  Heart,
  Brain,
  Briefcase,
  DollarSign,
  Users,
  Gamepad2,
  TrendingUp,
  Globe,
  Palette,
  Home,
  type LucideIcon,
} from 'lucide-react';

// Icon mapping for dimension codes
const dimensionIconMap: Record<string, LucideIcon> = {
  health: Heart,
  mind: Brain,
  work: Briefcase,
  money: DollarSign,
  relationships: Users,
  play: Gamepad2,
  growth: TrendingUp,
  community: Globe,
  create: Palette,
  assets: Home,
};

// Color mapping for dimension codes
const dimensionColorMap: Record<string, string> = {
  health: '#22c55e',
  mind: '#8b5cf6',
  work: '#f97316',
  money: '#eab308',
  relationships: '#ec4899',
  play: '#22d3ee',
  growth: '#6366f1',
  community: '#14b8a6',
  create: '#a855f7',
  assets: '#64748b',
};

// Default fallback icon and color
const defaultIcon = TrendingUp;
const defaultColor = '#6b7280';

/**
 * Get the Lucide icon component for a dimension code
 * @param code - The dimension code (e.g., 'health', 'mind')
 * @returns The corresponding Lucide icon component
 */
export function getDimensionIcon(code: string): LucideIcon {
  const normalizedCode = code.toLowerCase();
  return dimensionIconMap[normalizedCode] || defaultIcon;
}

/**
 * Get the color for a dimension code
 * @param code - The dimension code (e.g., 'health', 'mind')
 * @returns The hex color string for the dimension
 */
export function getDimensionColor(code: string): string {
  const normalizedCode = code.toLowerCase();
  return dimensionColorMap[normalizedCode] || defaultColor;
}

/**
 * Get both icon and color for a dimension code
 * @param code - The dimension code (e.g., 'health', 'mind')
 * @returns Object containing icon component and color string
 */
export function getDimensionIconAndColor(code: string): {
  icon: LucideIcon;
  color: string;
} {
  return {
    icon: getDimensionIcon(code),
    color: getDimensionColor(code),
  };
}
