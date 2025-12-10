import { useState, useEffect } from 'react';
import { ChevronDown, ChevronUp, Info, Lightbulb } from 'lucide-react';
import { GlassCard } from '@components/atoms/GlassCard';
import type { DimensionInfo } from '@/types';

interface DimensionInfoSectionProps {
  dimensionCode: string;
}

// Dimension info content stored as constants
const DIMENSION_INFO: Record<string, DimensionInfo> = {
  health: {
    dimensionCode: 'health',
    title: 'What is Health?',
    description: 'The Health dimension tracks your physical well-being and fitness levels. This encompasses all aspects of your physical body including exercise, nutrition, sleep, and medical health.\n\nBy tracking habits and metrics in this dimension, you gain visibility into patterns that affect your energy levels, longevity, and quality of life.',
    keyAreas: ['Exercise & Physical Activity', 'Nutrition & Diet', 'Sleep Quality', 'Medical & Preventive Care'],
    tips: ['Start with one simple daily habit like walking', 'Track metrics that matter most to your goals', 'Connect habits to your milestones for better motivation'],
  },
  mind: {
    dimensionCode: 'mind',
    title: 'What is Mind?',
    description: 'The Mind dimension focuses on your mental and emotional well-being. This includes mindfulness practices, stress management, emotional intelligence, and psychological health.\n\nMaintaining a healthy mind is foundational to success in all other life dimensions.',
    keyAreas: ['Mindfulness & Meditation', 'Stress Management', 'Emotional Intelligence', 'Mental Health'],
    tips: ['Practice daily meditation or mindfulness', 'Journal to process emotions', 'Seek professional support when needed'],
  },
  work: {
    dimensionCode: 'work',
    title: 'What is Work?',
    description: 'The Work dimension covers your professional life and career development. This includes job performance, skills development, career progression, and work-life balance.\n\nAligning your work with your values and goals leads to greater fulfillment and success.',
    keyAreas: ['Career Goals', 'Skills Development', 'Productivity', 'Work-Life Balance'],
    tips: ['Set clear career milestones', 'Dedicate time to skill development', 'Track your key performance metrics'],
  },
  money: {
    dimensionCode: 'money',
    title: 'What is Money?',
    description: 'The Money dimension tracks your financial health and wealth building. This includes income, savings, investments, debt management, and financial planning.\n\nFinancial security enables freedom and opportunity in all other life areas.',
    keyAreas: ['Income & Earnings', 'Savings & Emergency Fund', 'Investments', 'Debt Management'],
    tips: ['Automate your savings', 'Track your net worth monthly', 'Set specific financial milestones'],
  },
  relationships: {
    dimensionCode: 'relationships',
    title: 'What is Relationships?',
    description: 'The Relationships dimension focuses on your connections with family, friends, and romantic partners. Healthy relationships are fundamental to happiness and well-being.\n\nNurturing meaningful connections requires intentional effort and time.',
    keyAreas: ['Family', 'Friendships', 'Romantic Relationships', 'Social Network'],
    tips: ['Schedule regular quality time with loved ones', 'Practice active listening', 'Express gratitude regularly'],
  },
  play: {
    dimensionCode: 'play',
    title: 'What is Play?',
    description: 'The Play dimension encompasses recreation, hobbies, and fun activities. Play is essential for creativity, stress relief, and overall life satisfaction.\n\nBalancing work with play leads to a more fulfilling and sustainable life.',
    keyAreas: ['Hobbies & Interests', 'Recreation', 'Entertainment', 'Creative Pursuits'],
    tips: ['Schedule play time like you would work', 'Try new activities regularly', 'Engage in activities that bring you joy'],
  },
  growth: {
    dimensionCode: 'growth',
    title: 'What is Growth?',
    description: 'The Growth dimension focuses on personal development and continuous learning. This includes education, self-improvement, spiritual growth, and expanding your comfort zone.\n\nCommitting to lifelong growth keeps life engaging and meaningful.',
    keyAreas: ['Learning & Education', 'Self-Improvement', 'Spiritual Growth', 'Personal Challenges'],
    tips: ['Read regularly', 'Take courses in areas of interest', 'Set personal development goals'],
  },
  community: {
    dimensionCode: 'community',
    title: 'What is Community?',
    description: 'The Community dimension covers your contribution to society and connection to groups larger than yourself. This includes volunteering, civic engagement, and belonging to communities.\n\nContributing to others creates meaning and strengthens social bonds.',
    keyAreas: ['Volunteering', 'Civic Engagement', 'Group Memberships', 'Social Impact'],
    tips: ['Find causes you care about', 'Join communities aligned with your interests', 'Give back regularly'],
  },
};

export function DimensionInfoSection({ dimensionCode }: DimensionInfoSectionProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  
  // Load collapsed state from localStorage
  useEffect(() => {
    const stored = localStorage.getItem(`dimension-info-${dimensionCode}`);
    if (stored !== null) {
      setIsExpanded(stored === 'true');
    }
  }, [dimensionCode]);

  const toggleExpanded = () => {
    const newState = !isExpanded;
    setIsExpanded(newState);
    localStorage.setItem(`dimension-info-${dimensionCode}`, String(newState));
  };

  const info = DIMENSION_INFO[dimensionCode];
  
  if (!info) {
    return null;
  }

  return (
    <GlassCard variant="default" className="overflow-hidden">
      {/* Header - always visible */}
      <button
        onClick={toggleExpanded}
        className="w-full flex items-center justify-between p-4 hover:bg-glass-light transition-colors"
      >
        <div className="flex items-center gap-2">
          <Info className="w-4 h-4 text-accent-purple" />
          <span className="font-medium text-text-primary">{info.title}</span>
        </div>
        {isExpanded ? (
          <ChevronUp className="w-5 h-5 text-text-tertiary" />
        ) : (
          <ChevronDown className="w-5 h-5 text-text-tertiary" />
        )}
      </button>

      {/* Expandable content */}
      <div
        className={`transition-all duration-200 ease-in-out overflow-hidden ${
          isExpanded ? 'max-h-[500px] opacity-100' : 'max-h-0 opacity-0'
        }`}
      >
        <div className="px-4 pb-4 space-y-4">
          {/* Description */}
          <p className="text-text-secondary text-sm whitespace-pre-line">
            {info.description}
          </p>

          {/* Key Areas */}
          <div>
            <h4 className="text-xs font-semibold text-text-tertiary uppercase tracking-wide mb-2">
              Key Areas
            </h4>
            <div className="flex flex-wrap gap-2">
              {info.keyAreas.map((area) => (
                <span
                  key={area}
                  className="px-2 py-1 bg-glass-light rounded-full text-xs text-text-secondary"
                >
                  {area}
                </span>
              ))}
            </div>
          </div>

          {/* Tips */}
          {info.tips && info.tips.length > 0 && (
            <div>
              <div className="flex items-center gap-1 mb-2">
                <Lightbulb className="w-3 h-3 text-yellow-500" />
                <h4 className="text-xs font-semibold text-text-tertiary uppercase tracking-wide">
                  Tips
                </h4>
              </div>
              <ul className="space-y-1">
                {info.tips.map((tip, index) => (
                  <li key={index} className="text-xs text-text-secondary flex items-start gap-2">
                    <span className="text-accent-purple">•</span>
                    {tip}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      </div>
    </GlassCard>
  );
}
