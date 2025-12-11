import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  useGetOnboardingStatusQuery,
  useSubmitHealthBaselinesMutation,
  useSubmitMajorGoalsMutation,
  useSubmitIdentityMutation,
  useCompleteOnboardingMutation,
} from '../services/endpoints';
import { GlassCard } from '@components/atoms/GlassCard';
import { Heart, Target, Sparkles, ChevronRight, Check } from 'lucide-react';

type OnboardingStep = 'health_baselines' | 'major_goals' | 'identity' | 'complete';

// Health Baselines Form
function HealthBaselinesStep({ onNext }: { onNext: () => void }) {
  const [submitBaselines, { isLoading }] = useSubmitHealthBaselinesMutation();
  const [form, setForm] = useState({
    currentWeight: 80,
    targetWeight: 74,
    currentBodyFat: 20,
    targetBodyFat: 14,
    height: 180,
    birthDate: '1990-01-01',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await submitBaselines(form);
    onNext();
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-red-500 to-pink-500 flex items-center justify-center">
          <Heart className="w-6 h-6 text-white" />
        </div>
        <div>
          <h2 className="text-2xl font-bold text-text-primary">Health Baselines</h2>
          <p className="text-text-secondary text-sm">Let's start with your current health metrics and goals.</p>
        </div>
      </div>
      
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-text-secondary mb-1">Current Weight (kg)</label>
          <input
            type="number"
            value={form.currentWeight}
            onChange={(e) => setForm({ ...form, currentWeight: parseFloat(e.target.value) })}
            className="w-full bg-background-card border border-background-hover rounded-lg px-4 py-3 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-cyan/50"
            required
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-text-secondary mb-1">Target Weight (kg)</label>
          <input
            type="number"
            value={form.targetWeight}
            onChange={(e) => setForm({ ...form, targetWeight: parseFloat(e.target.value) })}
            className="w-full bg-background-card border border-background-hover rounded-lg px-4 py-3 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-cyan/50"
            required
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-text-secondary mb-1">Current Body Fat (%)</label>
          <input
            type="number"
            value={form.currentBodyFat}
            onChange={(e) => setForm({ ...form, currentBodyFat: parseFloat(e.target.value) })}
            className="w-full bg-background-card border border-background-hover rounded-lg px-4 py-3 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-cyan/50"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-text-secondary mb-1">Target Body Fat (%)</label>
          <input
            type="number"
            value={form.targetBodyFat}
            onChange={(e) => setForm({ ...form, targetBodyFat: parseFloat(e.target.value) })}
            className="w-full bg-background-card border border-background-hover rounded-lg px-4 py-3 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-cyan/50"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-text-secondary mb-1">Height (cm)</label>
          <input
            type="number"
            value={form.height}
            onChange={(e) => setForm({ ...form, height: parseFloat(e.target.value) })}
            className="w-full bg-background-card border border-background-hover rounded-lg px-4 py-3 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-cyan/50"
            required
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-text-secondary mb-1">Birth Date</label>
          <input
            type="date"
            value={form.birthDate}
            onChange={(e) => setForm({ ...form, birthDate: e.target.value })}
            className="w-full bg-background-card border border-background-hover rounded-lg px-4 py-3 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-cyan/50"
          />
        </div>
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-gradient-to-r from-accent-cyan to-accent-purple hover:opacity-90 text-white font-medium py-3 rounded-lg transition-all flex items-center justify-center gap-2"
      >
        {isLoading ? 'Saving...' : 'Continue'}
        <ChevronRight className="w-5 h-5" />
      </button>
    </form>
  );
}

// Major Goals Form
function MajorGoalsStep({ onNext }: { onNext: () => void }) {
  const [submitGoals, { isLoading }] = useSubmitMajorGoalsMutation();
  const [financialGoals, setFinancialGoals] = useState([
    { description: 'First million', targetAmount: 1000000, targetAge: 40 },
  ]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await submitGoals({ financialGoals, lifeMilestones: [] });
    onNext();
  };

  const addGoal = () => {
    setFinancialGoals([...financialGoals, { description: '', targetAmount: 0, targetAge: 50 }]);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-amber-500 to-orange-500 flex items-center justify-center">
          <Target className="w-6 h-6 text-white" />
        </div>
        <div>
          <h2 className="text-2xl font-bold text-text-primary">Major Goals</h2>
          <p className="text-text-secondary text-sm">What are your big financial and life goals?</p>
        </div>
      </div>

      {financialGoals.map((goal, index) => (
        <GlassCard key={index} className="p-4 space-y-3">
          <input
            type="text"
            placeholder="Goal description (e.g., 'Net worth 1M')"
            value={goal.description}
            onChange={(e) => {
              const updated = [...financialGoals];
              updated[index].description = e.target.value;
              setFinancialGoals(updated);
            }}
            className="w-full bg-background border border-background-hover rounded-lg px-4 py-3 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-cyan/50"
          />
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-text-tertiary mb-1">Target Amount</label>
              <input
                type="number"
                value={goal.targetAmount}
                onChange={(e) => {
                  const updated = [...financialGoals];
                  updated[index].targetAmount = parseFloat(e.target.value);
                  setFinancialGoals(updated);
                }}
                className="w-full bg-background border border-background-hover rounded-lg px-4 py-3 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-cyan/50"
              />
            </div>
            <div>
              <label className="block text-sm text-text-tertiary mb-1">Target Age</label>
              <input
                type="number"
                value={goal.targetAge}
                onChange={(e) => {
                  const updated = [...financialGoals];
                  updated[index].targetAge = parseInt(e.target.value);
                  setFinancialGoals(updated);
                }}
                className="w-full bg-background border border-background-hover rounded-lg px-4 py-3 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-cyan/50"
              />
            </div>
          </div>
        </GlassCard>
      ))}

      <button
        type="button"
        onClick={addGoal}
        className="text-accent-cyan hover:text-accent-purple text-sm font-medium transition-colors"
      >
        + Add another goal
      </button>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-gradient-to-r from-accent-cyan to-accent-purple hover:opacity-90 text-white font-medium py-3 rounded-lg transition-all flex items-center justify-center gap-2"
      >
        {isLoading ? 'Saving...' : 'Continue'}
        <ChevronRight className="w-5 h-5" />
      </button>
    </form>
  );
}

// Identity Step
function IdentityStep({ onNext }: { onNext: () => void }) {
  const [submitIdentity, { isLoading }] = useSubmitIdentityMutation();
  const [form, setForm] = useState({
    archetype: 'God of Mind-Power',
    values: ['discipline', 'growth', 'impact', 'freedom'],
    primaryStatFocus: ['wisdom', 'vitality'],
  });

  const archetypes = [
    'God of Mind-Power',
    'Balanced Achiever',
    'Wealth Builder',
    'Health Optimizer',
    'Creative Master',
    'Community Leader',
  ];

  const allValues = [
    'discipline', 'growth', 'impact', 'freedom', 'health', 'wealth',
    'creativity', 'relationships', 'adventure', 'knowledge', 'peace', 'power',
  ];

  const allStats = ['strength', 'wisdom', 'charisma', 'composure', 'energy', 'influence', 'vitality'];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await submitIdentity(form);
    onNext();
  };

  const toggleValue = (value: string) => {
    if (form.values.includes(value)) {
      setForm({ ...form, values: form.values.filter(v => v !== value) });
    } else if (form.values.length < 5) {
      setForm({ ...form, values: [...form.values, value] });
    }
  };

  const toggleStat = (stat: string) => {
    if (form.primaryStatFocus.includes(stat)) {
      setForm({ ...form, primaryStatFocus: form.primaryStatFocus.filter(s => s !== stat) });
    } else if (form.primaryStatFocus.length < 3) {
      setForm({ ...form, primaryStatFocus: [...form.primaryStatFocus, stat] });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-violet-500 to-purple-500 flex items-center justify-center">
          <Sparkles className="w-6 h-6 text-white" />
        </div>
        <div>
          <h2 className="text-2xl font-bold text-text-primary">Your Identity</h2>
          <p className="text-text-secondary text-sm">Define your target persona and what matters most.</p>
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-text-secondary mb-2">Archetype</label>
        <div className="grid grid-cols-2 gap-2">
          {archetypes.map(archetype => (
            <button
              key={archetype}
              type="button"
              onClick={() => setForm({ ...form, archetype })}
              className={`p-3 rounded-lg text-sm font-medium transition-all ${
                form.archetype === archetype
                  ? 'bg-gradient-to-r from-accent-purple to-violet-600 text-white shadow-lg shadow-accent-purple/30'
                  : 'bg-background-card text-text-secondary hover:bg-background-hover border border-background-hover'
              }`}
            >
              {archetype}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-text-secondary mb-2">
          Core Values <span className="text-text-tertiary">(select up to 5)</span>
        </label>
        <div className="flex flex-wrap gap-2">
          {allValues.map(value => (
            <button
              key={value}
              type="button"
              onClick={() => toggleValue(value)}
              className={`px-3 py-1.5 rounded-full text-sm font-medium transition-all ${
                form.values.includes(value)
                  ? 'bg-accent-cyan text-white'
                  : 'bg-background-card text-text-secondary hover:bg-background-hover border border-background-hover'
              }`}
            >
              {value}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-text-secondary mb-2">
          Primary Stat Focus <span className="text-text-tertiary">(select up to 3)</span>
        </label>
        <div className="flex flex-wrap gap-2">
          {allStats.map(stat => (
            <button
              key={stat}
              type="button"
              onClick={() => toggleStat(stat)}
              className={`px-3 py-1.5 rounded-full text-sm font-medium capitalize transition-all ${
                form.primaryStatFocus.includes(stat)
                  ? 'bg-gradient-to-r from-accent-purple to-violet-600 text-white'
                  : 'bg-background-card text-text-secondary hover:bg-background-hover border border-background-hover'
              }`}
            >
              {stat}
            </button>
          ))}
        </div>
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-gradient-to-r from-accent-cyan to-accent-purple hover:opacity-90 text-white font-medium py-3 rounded-lg transition-all flex items-center justify-center gap-2"
      >
        {isLoading ? 'Saving...' : 'Complete Setup'}
        <Check className="w-5 h-5" />
      </button>
    </form>
  );
}

// Main Onboarding Page
export function Onboarding() {
  const navigate = useNavigate();
  const { refetch } = useGetOnboardingStatusQuery();
  const [completeOnboarding] = useCompleteOnboardingMutation();
  const [currentStep, setCurrentStep] = useState<OnboardingStep>('health_baselines');

  const handleNext = async () => {
    await refetch();
    if (currentStep === 'health_baselines') {
      setCurrentStep('major_goals');
    } else if (currentStep === 'major_goals') {
      setCurrentStep('identity');
    } else if (currentStep === 'identity') {
      await completeOnboarding();
      navigate('/');
    }
  };

  const steps = [
    { key: 'health_baselines', label: 'Health', icon: Heart },
    { key: 'major_goals', label: 'Goals', icon: Target },
    { key: 'identity', label: 'Identity', icon: Sparkles },
  ];

  const currentStepIndex = steps.findIndex(s => s.key === currentStep);

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-6">
      <div className="w-full max-w-lg">
        {/* Progress indicator */}
        <div className="flex items-center justify-center mb-8">
          {steps.map((step, index) => {
            const Icon = step.icon;
            const isComplete = index < currentStepIndex;
            const isCurrent = index === currentStepIndex;
            
            return (
              <div key={step.key} className="flex items-center">
                <div
                  className={`w-10 h-10 rounded-full flex items-center justify-center transition-all ${
                    isComplete
                      ? 'bg-gradient-to-r from-accent-cyan to-accent-purple text-white'
                      : isCurrent
                        ? 'bg-accent-cyan text-white shadow-lg shadow-accent-cyan/30'
                        : 'bg-background-card text-text-tertiary border border-background-hover'
                  }`}
                >
                  {isComplete ? (
                    <Check className="w-5 h-5" />
                  ) : (
                    <Icon className="w-5 h-5" />
                  )}
                </div>
                {index < steps.length - 1 && (
                  <div
                    className={`w-16 h-1 mx-2 rounded-full transition-all ${
                      index < currentStepIndex
                        ? 'bg-gradient-to-r from-accent-cyan to-accent-purple'
                        : 'bg-background-hover'
                    }`}
                  />
                )}
              </div>
            );
          })}
        </div>

        {/* Step labels */}
        <div className="flex justify-between mb-8 px-2">
          {steps.map((step, index) => (
            <span 
              key={step.key}
              className={`text-sm font-medium transition-colors ${
                index <= currentStepIndex ? 'text-text-primary' : 'text-text-tertiary'
              }`}
            >
              {step.label}
            </span>
          ))}
        </div>

        {/* Step content */}
        <GlassCard variant="elevated" glow="accent" className="p-8">
          {currentStep === 'health_baselines' && <HealthBaselinesStep onNext={handleNext} />}
          {currentStep === 'major_goals' && <MajorGoalsStep onNext={handleNext} />}
          {currentStep === 'identity' && <IdentityStep onNext={handleNext} />}
        </GlassCard>

        {/* Footer */}
        <p className="text-center text-text-tertiary text-sm mt-6">
          Setting up your personal life operating system
        </p>
      </div>
    </div>
  );
}

export default Onboarding;
