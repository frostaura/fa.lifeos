import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import {
  ShoppingCart,
  Home,
  Target,
  TrendingUp,
  ArrowLeft,
  ArrowRight,
  Loader2,
  CheckCircle2,
} from 'lucide-react';
import { cn } from '@utils/cn';
import { formatCurrency } from '@components/molecules/CurrencySelector';
import {
  useCreateScenarioMutation,
  useAddEventMutation,
  useRunSimulationMutation,
  useGetAccountsQuery,
} from '@/services';
import type { Account } from '@/types';

type WizardTemplate = 'purchase' | 'loan' | 'zero' | 'target';
type WizardStep = 'select' | 'configure' | 'result';

interface TemplateCard {
  id: WizardTemplate;
  title: string;
  question: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
  color: string;
}

const templates: TemplateCard[] = [
  {
    id: 'purchase',
    title: 'Purchase Impact',
    question: 'How does buying X today affect my net worth in Y years?',
    description: 'See the compound cost of a purchase over time',
    icon: ShoppingCart,
    color: 'text-accent-orange',
  },
  {
    id: 'loan',
    title: 'Loan Payoff Date',
    question: 'At this rate, when will my loan be paid off?',
    description: 'Project when a specific loan reaches zero',
    icon: Home,
    color: 'text-accent-cyan',
  },
  {
    id: 'zero',
    title: 'Zero Net Worth',
    question: 'When will I reach $0 net worth?',
    description: 'For those in debt, see when you break even',
    icon: TrendingUp,
    color: 'text-semantic-warning',
  },
  {
    id: 'target',
    title: 'Target Net Worth',
    question: 'When will I reach my target net worth?',
    description: 'Set a goal and see when you\'ll reach it',
    icon: Target,
    color: 'text-accent-purple',
  },
];

interface SimulationResult {
  scenarioId: string;
  keyMilestones: Array<{
    description: string;
    date: string;
    value: number;
    yearsAway: number;
  }>;
}

export function SimulationWizard() {
  const navigate = useNavigate();
  const [step, setStep] = useState<WizardStep>('select');
  const [selectedTemplate, setSelectedTemplate] = useState<WizardTemplate | null>(null);
  const [result, setResult] = useState<SimulationResult | null>(null);

  // Form state for each template
  const [purchaseAmount, setPurchaseAmount] = useState<string>('');
  const [purchaseYears, setPurchaseYears] = useState<string>('10');
  const [selectedLoanId, setSelectedLoanId] = useState<string>('');
  const [targetAmount, setTargetAmount] = useState<string>('1000000');

  // API hooks
  const { data: accountsResponse } = useGetAccountsQuery();
  const accounts = accountsResponse?.data || [];
  const loanAccounts = accounts.filter((a: Account) => a.isLiability && (a.type === 'loan' || a.type === 'credit'));

  const [createScenario, { isLoading: creatingScenario }] = useCreateScenarioMutation();
  const [addEvent, { isLoading: addingEvent }] = useAddEventMutation();
  const [runSimulation, { isLoading: runningSimulation }] = useRunSimulationMutation();

  const isLoading = creatingScenario || addingEvent || runningSimulation;

  const handleTemplateSelect = (template: WizardTemplate) => {
    setSelectedTemplate(template);
    setStep('configure');
  };

  const handleBack = () => {
    if (step === 'configure') {
      setStep('select');
      setSelectedTemplate(null);
    } else if (step === 'result') {
      setStep('configure');
      setResult(null);
    }
  };

  const getEndDate = (years: number): string => {
    const date = new Date();
    date.setFullYear(date.getFullYear() + years);
    return date.toISOString().split('T')[0];
  };

  const runPurchaseSimulation = async () => {
    const amount = parseFloat(purchaseAmount);
    const years = parseInt(purchaseYears, 10);
    if (isNaN(amount) || isNaN(years)) return;

    const startDate = new Date().toISOString().split('T')[0];
    const endDate = getEndDate(years);

    // Create scenario
    const scenario = await createScenario({
      name: `Purchase Impact: R${amount.toLocaleString()}`,
      description: `Analyzing the impact of a R${amount.toLocaleString()} purchase over ${years} years`,
      startDate,
      endDate,
      isActive: false,
    }).unwrap();

    // Add purchase event
    await addEvent({
      scenarioId: scenario.id,
      event: {
        name: 'One-time Purchase',
        type: 'one_time_expense',
        date: startDate,
        amount,
        currency: 'ZAR',
        isRecurring: false,
      },
    }).unwrap();

    // Run simulation
    const simResult = await runSimulation(scenario.id).unwrap();
    setResult({
      scenarioId: scenario.id,
      keyMilestones: simResult.keyMilestones,
    });
    setStep('result');
  };

  const runLoanSimulation = async () => {
    if (!selectedLoanId) return;

    const selectedLoan = loanAccounts.find((a: Account) => a.id === selectedLoanId);
    if (!selectedLoan) return;

    const startDate = new Date().toISOString().split('T')[0];
    const endDate = getEndDate(30); // 30-year projection for loan payoff

    // Create scenario
    const scenario = await createScenario({
      name: `Loan Payoff: ${selectedLoan.name}`,
      description: `Projecting payoff date for ${selectedLoan.name}`,
      startDate,
      endDate,
      isActive: false,
    }).unwrap();

    // Run simulation
    const simResult = await runSimulation(scenario.id).unwrap();
    setResult({
      scenarioId: scenario.id,
      keyMilestones: simResult.keyMilestones,
    });
    setStep('result');
  };

  const runZeroNetWorthSimulation = async () => {
    const startDate = new Date().toISOString().split('T')[0];
    const endDate = getEndDate(30); // 30-year projection

    // Create scenario
    const scenario = await createScenario({
      name: 'Zero Net Worth Projection',
      description: 'Projecting when net worth reaches zero (debt-free)',
      startDate,
      endDate,
      isActive: false,
    }).unwrap();

    // Run simulation
    const simResult = await runSimulation(scenario.id).unwrap();
    setResult({
      scenarioId: scenario.id,
      keyMilestones: simResult.keyMilestones,
    });
    setStep('result');
  };

  const runTargetSimulation = async () => {
    const target = parseFloat(targetAmount);
    if (isNaN(target)) return;

    const startDate = new Date().toISOString().split('T')[0];
    const endDate = getEndDate(50); // 50-year projection for long-term goals

    // Create scenario
    const scenario = await createScenario({
      name: `Target: R${target.toLocaleString()}`,
      description: `Projecting when net worth reaches R${target.toLocaleString()}`,
      startDate,
      endDate,
      isActive: false,
    }).unwrap();

    // Run simulation
    const simResult = await runSimulation(scenario.id).unwrap();
    setResult({
      scenarioId: scenario.id,
      keyMilestones: simResult.keyMilestones,
    });
    setStep('result');
  };

  const handleRunSimulation = async () => {
    try {
      switch (selectedTemplate) {
        case 'purchase':
          await runPurchaseSimulation();
          break;
        case 'loan':
          await runLoanSimulation();
          break;
        case 'zero':
          await runZeroNetWorthSimulation();
          break;
        case 'target':
          await runTargetSimulation();
          break;
      }
    } catch (error) {
      console.error('Simulation failed:', error);
    }
  };

  const renderTemplateSelection = () => (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-text-primary">Simulation Wizard</h1>
        <p className="text-text-secondary">Choose a template to answer common financial questions</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {templates.map((template) => {
          const Icon = template.icon;
          return (
            <GlassCard
              key={template.id}
              variant="default"
              className="p-6 cursor-pointer hover:bg-background-hover transition-all group"
              onClick={() => handleTemplateSelect(template.id)}
            >
              <div className="flex items-start gap-4">
                <div className={cn('p-3 rounded-xl bg-glass-medium group-hover:bg-glass-heavy transition-colors', template.color)}>
                  <Icon className="w-6 h-6" />
                </div>
                <div className="flex-1">
                  <h3 className="font-semibold text-text-primary mb-1">{template.title}</h3>
                  <p className="text-sm text-text-secondary mb-2">{template.question}</p>
                  <p className="text-xs text-text-tertiary">{template.description}</p>
                </div>
                <ArrowRight className="w-5 h-5 text-text-tertiary group-hover:text-text-secondary transition-colors" />
              </div>
            </GlassCard>
          );
        })}
      </div>

      <div className="pt-4 border-t border-glass-border">
        <Button
          variant="ghost"
          onClick={() => navigate('/simulation/builder')}
        >
          Or build a custom scenario →
        </Button>
      </div>
    </div>
  );

  const renderPurchaseForm = () => (
    <div className="space-y-4">
      <Input
        label="Purchase Amount (ZAR)"
        type="number"
        placeholder="e.g., 50000"
        value={purchaseAmount}
        onChange={(e) => setPurchaseAmount(e.target.value)}
      />
      <Input
        label="Time Horizon (Years)"
        type="number"
        placeholder="e.g., 10"
        value={purchaseYears}
        onChange={(e) => setPurchaseYears(e.target.value)}
      />
      <p className="text-sm text-text-tertiary">
        This will show you how a purchase of R{purchaseAmount || '0'} today would affect your net worth after {purchaseYears || '0'} years, considering compound growth.
      </p>
    </div>
  );

  const renderLoanForm = () => (
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-text-secondary mb-1.5">
          Select Loan Account
        </label>
        {loanAccounts.length === 0 ? (
          <p className="text-sm text-text-tertiary p-4 bg-glass-light rounded-lg">
            No loan accounts found. Add a loan account in Finances first.
          </p>
        ) : (
          <div className="space-y-2">
            {loanAccounts.map((loan: Account) => (
              <GlassCard
                key={loan.id}
                variant={selectedLoanId === loan.id ? 'elevated' : 'default'}
                className={cn(
                  'p-4 cursor-pointer transition-all',
                  selectedLoanId === loan.id ? 'ring-2 ring-accent-purple' : 'hover:bg-background-hover'
                )}
                onClick={() => setSelectedLoanId(loan.id)}
              >
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-medium text-text-primary">{loan.name}</p>
                    <p className="text-sm text-text-tertiary">{loan.type}</p>
                  </div>
                  <p className="text-lg font-semibold text-semantic-error">
                    {formatCurrency(Math.abs(loan.balance), loan.currency)}
                  </p>
                </div>
              </GlassCard>
            ))}
          </div>
        )}
      </div>
    </div>
  );

  const renderZeroForm = () => (
    <div className="space-y-4">
      <div className="p-4 bg-glass-light rounded-lg">
        <p className="text-text-secondary">
          This simulation will project when your net worth reaches zero, based on your current accounts and cash flow.
        </p>
      </div>
      <p className="text-sm text-text-tertiary">
        No additional inputs needed. The simulation uses your current financial data.
      </p>
    </div>
  );

  const renderTargetForm = () => (
    <div className="space-y-4">
      <Input
        label="Target Net Worth (ZAR)"
        type="number"
        placeholder="e.g., 1000000"
        value={targetAmount}
        onChange={(e) => setTargetAmount(e.target.value)}
      />
      <p className="text-sm text-text-tertiary">
        This will project when you'll reach a net worth of R{parseInt(targetAmount || '0', 10).toLocaleString()} based on your current trajectory.
      </p>
    </div>
  );

  const renderConfigureStep = () => {
    const template = templates.find((t) => t.id === selectedTemplate);
    if (!template) return null;

    const Icon = template.icon;

    const isValid = () => {
      switch (selectedTemplate) {
        case 'purchase':
          return purchaseAmount && purchaseYears;
        case 'loan':
          return selectedLoanId;
        case 'zero':
          return true;
        case 'target':
          return targetAmount;
        default:
          return false;
      }
    };

    return (
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center gap-4">
          <button
            onClick={handleBack}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <ArrowLeft className="w-5 h-5 text-text-secondary" />
          </button>
          <div className={cn('p-3 rounded-xl bg-glass-medium', template.color)}>
            <Icon className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-text-primary">{template.title}</h1>
            <p className="text-text-secondary">{template.question}</p>
          </div>
        </div>

        {/* Form */}
        <GlassCard variant="default" className="p-6">
          {selectedTemplate === 'purchase' && renderPurchaseForm()}
          {selectedTemplate === 'loan' && renderLoanForm()}
          {selectedTemplate === 'zero' && renderZeroForm()}
          {selectedTemplate === 'target' && renderTargetForm()}
        </GlassCard>

        {/* Actions */}
        <div className="flex justify-end gap-3">
          <Button variant="ghost" onClick={handleBack}>
            Back
          </Button>
          <Button
            onClick={handleRunSimulation}
            disabled={!isValid() || isLoading}
          >
            {isLoading ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Running Simulation...
              </>
            ) : (
              'Run Simulation'
            )}
          </Button>
        </div>
      </div>
    );
  };

  const renderResultStep = () => {
    const template = templates.find((t) => t.id === selectedTemplate);
    if (!template || !result) return null;

    const Icon = template.icon;

    return (
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center gap-4">
          <button
            onClick={handleBack}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <ArrowLeft className="w-5 h-5 text-text-secondary" />
          </button>
          <div className="p-3 rounded-xl bg-semantic-success/20 text-semantic-success">
            <CheckCircle2 className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-text-primary">Simulation Complete</h1>
            <p className="text-text-secondary">{template.title} results</p>
          </div>
        </div>

        {/* Results Summary */}
        <GlassCard variant="elevated" className="p-6">
          <div className="flex items-center gap-3 mb-4">
            <div className={cn('p-2 rounded-lg bg-glass-medium', template.color)}>
              <Icon className="w-5 h-5" />
            </div>
            <h2 className="text-lg font-semibold text-text-primary">Key Milestones</h2>
          </div>

          {result.keyMilestones.length === 0 ? (
            <p className="text-text-tertiary">No key milestones found in the projection period.</p>
          ) : (
            <div className="space-y-3">
              {result.keyMilestones.map((milestone, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-4 rounded-lg bg-glass-light"
                >
                  <div>
                    <p className="font-medium text-text-primary">{milestone.description}</p>
                    <p className="text-sm text-text-tertiary">
                      {new Date(milestone.date).toLocaleDateString('en-ZA', {
                        year: 'numeric',
                        month: 'long',
                        day: 'numeric',
                      })}
                      {milestone.yearsAway > 0 && ` (${milestone.yearsAway.toFixed(1)} years away)`}
                    </p>
                  </div>
                  <p className={cn(
                    'text-lg font-semibold',
                    milestone.value >= 0 ? 'text-semantic-success' : 'text-semantic-error'
                  )}>
                    {formatCurrency(milestone.value, 'ZAR')}
                  </p>
                </div>
              ))}
            </div>
          )}
        </GlassCard>

        {/* Actions */}
        <div className="flex justify-between gap-3">
          <Button variant="ghost" onClick={() => {
            setStep('select');
            setSelectedTemplate(null);
            setResult(null);
          }}>
            Run Another Simulation
          </Button>
          <Button onClick={() => navigate(`/simulation/${result.scenarioId}`)}>
            View Full Details →
          </Button>
        </div>
      </div>
    );
  };

  return (
    <div className="space-y-6">
      {step === 'select' && renderTemplateSelection()}
      {step === 'configure' && renderConfigureStep()}
      {step === 'result' && renderResultStep()}
    </div>
  );
}
