import { useState, useEffect, useCallback, useRef } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { InfoTooltip } from '@components/atoms/InfoTooltip';
import { User, Key, Grid3X3, Calculator, DollarSign, Copy, Trash2, Plus, Check, Loader2, AlertCircle, TrendingUp, Target, Download, Upload, ArrowUpDown } from 'lucide-react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { cn } from '@utils/cn';
import { confirmToast } from '@utils/confirmToast';
import { TOOLTIP_CONTENT } from '@utils/tooltipContent';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from 'recharts';
import { useBreakpoint } from '@/hooks/useBreakpoint';
import {
  useGetProfileQuery,
  useUpdateProfileMutation,
  useUpdateDimensionWeightsMutation,
  useGetApiKeysQuery,
  useCreateApiKeyMutation,
  useRevokeApiKeyMutation,
  useGetTaxProfilesQuery,
  useCreateTaxProfileMutation,
  useUpdateTaxProfileMutation,
  useDeleteTaxProfileMutation,
  useGetIncomeSourcesWithSummaryQuery,
  useCreateIncomeSourceMutation,
  useUpdateIncomeSourceMutation,
  useDeleteIncomeSourceMutation,
  useGetExpenseDefinitionsQuery,
  useCreateExpenseDefinitionMutation,
  useUpdateExpenseDefinitionMutation,
  useDeleteExpenseDefinitionMutation,
  useGetInvestmentContributionsQuery,
  useCreateInvestmentContributionMutation,
  useUpdateInvestmentContributionMutation,
  useDeleteInvestmentContributionMutation,
  useGetFinancialGoalsQuery,
  useCreateFinancialGoalMutation,
  useUpdateFinancialGoalMutation,
  useDeleteFinancialGoalMutation,
  useGetAccountsQuery,
  useImportDataFileMutation,
} from '@/services/endpoints';
import type { 
  TaxProfile, 
  TaxBracket, 
  CreateTaxProfileRequest,
  IncomeSource,
  ExpenseDefinition,
  CreateIncomeSourceRequest,
  CreateExpenseDefinitionRequest,
  PaymentFrequency,
  InvestmentContribution,
  CreateInvestmentContributionRequest,
  FinancialGoal,
  CreateFinancialGoalRequest,
  EndConditionType,
} from '@/types';

const settingsNav = [
  { icon: User, label: 'Profile', path: '/settings/profile' },
  { icon: Key, label: 'API Keys', path: '/settings/api-keys' },
  { icon: Grid3X3, label: 'Dimensions', path: '/settings/dimensions' },
  { icon: Download, label: 'Data Portability', path: '/settings/data' },
];

export function Settings() {
  const location = useLocation();
  const isRootSettings = location.pathname === '/settings' || location.pathname === '/settings/';

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl md:text-2xl lg:text-3xl font-bold text-text-primary whitespace-nowrap">Settings</h1>
        <p className="text-text-secondary mt-1 text-sm md:text-base whitespace-nowrap">Manage your preferences</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Settings Navigation */}
        <GlassCard variant="default" className="p-3 md:p-4 h-fit">
          <nav className="space-y-1">
            {settingsNav.map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-2 md:gap-3 px-2 md:px-3 py-2 md:py-2.5 rounded-lg transition-colors',
                    isActive
                      ? 'bg-accent-purple/20 text-accent-purple'
                      : 'text-text-secondary hover:text-text-primary hover:bg-background-hover'
                  )
                }
              >
                <item.icon className="w-4 h-4 md:w-5 md:h-5" />
                <span className="font-medium text-xs md:text-sm whitespace-nowrap">{item.label}</span>
              </NavLink>
            ))}
          </nav>
        </GlassCard>

        {/* Settings Content */}
        <div className="lg:col-span-3">
          {isRootSettings ? <ProfileSettings /> : <Outlet />}
        </div>
      </div>
    </div>
  );
}

export function ProfileSettings() {
  const { data: profile, isLoading, error } = useGetProfileQuery();
  const [updateProfile, { isLoading: isSaving }] = useUpdateProfileMutation();
  const [updateWeights] = useUpdateDimensionWeightsMutation();
  
  const [formData, setFormData] = useState({
    homeCurrency: 'ZAR',
    dateOfBirth: '',
    username: '',
  });
  
  const [weights, setWeights] = useState<Record<string, number>>({});
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    if (profile) {
      setFormData({
        homeCurrency: profile.homeCurrency || 'ZAR',
        dateOfBirth: profile.dateOfBirth || '',
        username: profile.username || '',
      });
      
      const initialWeights: Record<string, number> = {};
      profile.dimensions.forEach(dim => {
        initialWeights[dim.id] = Math.round(dim.weight * 100);
      });
      setWeights(initialWeights);
    }
  }, [profile]);

  const handleSave = async () => {
    setSaveError(null);
    setSaveSuccess(false);
    
    try {
      // Update profile
      await updateProfile({
        homeCurrency: formData.homeCurrency,
        dateOfBirth: formData.dateOfBirth || undefined,
        username: formData.username || undefined,
      }).unwrap();
      
      // Update dimension weights
      const weightUpdates = Object.entries(weights).map(([dimensionId, weight]) => ({
        dimensionId,
        weight,
      }));
      
      if (weightUpdates.length > 0) {
        await updateWeights(weightUpdates).unwrap();
      }
      
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (err) {
      setSaveError('Failed to save settings. Please try again.');
    }
  };

  const totalWeight = Object.values(weights).reduce((sum, w) => sum + w, 0);
  const isValidWeight = Math.abs(totalWeight - 100) < 0.01;

  if (isLoading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-8 h-8 animate-spin text-accent-purple" />
        </div>
      </GlassCard>
    );
  }

  if (error) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center gap-3 text-red-400">
          <AlertCircle className="w-5 h-5" />
          <span>Failed to load profile settings</span>
        </div>
      </GlassCard>
    );
  }

  return (
    <GlassCard variant="default" className="p-6">
      <h2 className="text-xl font-semibold text-text-primary mb-6">Profile Settings</h2>

      <div className="space-y-6">
        <div>
          <label className="block text-sm font-medium text-text-secondary mb-2">
            Email Address
          </label>
          <input
            type="email"
            value={profile?.email || ''}
            disabled
            className="w-full bg-bg-tertiary/50 border border-glass-border rounded-lg px-4 py-2.5 text-text-secondary cursor-not-allowed"
          />
          <p className="text-xs text-text-secondary mt-1">Email cannot be changed</p>
        </div>

        <div>
          <label className="block text-sm font-medium text-text-secondary mb-2">
            Display Name
          </label>
          <input
            type="text"
            value={formData.username}
            onChange={(e) => setFormData(prev => ({ ...prev, username: e.target.value }))}
            placeholder="Your name"
            className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-4 py-2.5 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-text-secondary mb-2">
            <span className="inline-flex items-center gap-1.5">
              Home Currency
              <InfoTooltip content={TOOLTIP_CONTENT.homeCurrency} />
            </span>
          </label>
          <select
            value={formData.homeCurrency}
            onChange={(e) => setFormData(prev => ({ ...prev, homeCurrency: e.target.value }))}
            className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-4 py-2.5 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
          >
            <option value="ZAR">ZAR - South African Rand</option>
            <option value="USD">USD - US Dollar</option>
            <option value="EUR">EUR - Euro</option>
            <option value="GBP">GBP - British Pound</option>
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-text-secondary mb-2">
            Date of Birth
          </label>
          <input
            type="date"
            value={formData.dateOfBirth}
            onChange={(e) => setFormData(prev => ({ ...prev, dateOfBirth: e.target.value }))}
            className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-4 py-2.5 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-text-secondary mb-4">
            <span className="inline-flex items-center gap-1.5">
              Dimension Weights
              <InfoTooltip content={TOOLTIP_CONTENT.dimensionWeights} />
            </span>
            <span className={cn(
              "ml-2 text-xs",
              isValidWeight ? "text-green-400" : "text-red-400"
            )}>
              (Total: {totalWeight}% - {isValidWeight ? '✓' : 'must equal 100%'})
            </span>
          </label>
          <div className="space-y-3">
            {profile?.dimensions.map((dim) => (
              <div key={dim.id} className="flex items-center gap-4">
                <span className="w-36 text-text-primary text-sm">{dim.name}</span>
                <input
                  type="range"
                  min="0"
                  max="30"
                  value={weights[dim.id] || 0}
                  onChange={(e) => setWeights(prev => ({
                    ...prev,
                    [dim.id]: parseInt(e.target.value)
                  }))}
                  className="flex-1 accent-accent-purple"
                />
                <span className="w-12 text-right text-text-secondary text-sm">
                  {weights[dim.id] || 0}%
                </span>
              </div>
            ))}
          </div>
        </div>

        {saveError && (
          <div className="flex items-center gap-2 text-red-400 text-sm">
            <AlertCircle className="w-4 h-4" />
            {saveError}
          </div>
        )}

        {saveSuccess && (
          <div className="flex items-center gap-2 text-green-400 text-sm">
            <Check className="w-4 h-4" />
            Settings saved successfully!
          </div>
        )}

        <button
          onClick={handleSave}
          disabled={isSaving || !isValidWeight}
          className={cn(
            "px-6 py-2.5 rounded-lg text-white font-medium transition-colors flex items-center gap-2",
            isSaving || !isValidWeight
              ? "bg-gray-600 cursor-not-allowed"
              : "bg-accent-purple hover:bg-accent-purple/80"
          )}
        >
          {isSaving ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              Saving...
            </>
          ) : (
            'Save Changes'
          )}
        </button>
      </div>
    </GlassCard>
  );
}

export function ApiKeySettings() {
  const { data: apiKeys, isLoading } = useGetApiKeysQuery();
  const [createApiKey, { isLoading: isCreating }] = useCreateApiKeyMutation();
  const [revokeApiKey] = useRevokeApiKeyMutation();
  
  const [newKeyName, setNewKeyName] = useState('');
  const [newKey, setNewKey] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const handleCreateKey = async () => {
    try {
      const result = await createApiKey({
        name: newKeyName || 'API Key',
        scopes: 'metrics:write,webhooks:trigger',
      }).unwrap();
      
      setNewKey(result.key);
      setNewKeyName('');
    } catch (err) {
      console.error('Failed to create API key:', err);
    }
  };

  const handleCopyKey = useCallback(() => {
    if (newKey) {
      navigator.clipboard.writeText(newKey);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  }, [newKey]);

  const handleRevoke = async (id: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to revoke this API key? This cannot be undone.',
    });
    if (confirmed) {
      await revokeApiKey(id);
    }
  };

  return (
    <GlassCard variant="default" className="p-6">
      <h2 className="text-xl font-semibold text-text-primary mb-6">API Keys</h2>
      
      <p className="text-text-secondary mb-6">
        Generate API keys for automation tools like n8n or iOS Shortcuts.
        Keys are shown only once when created.
      </p>

      {/* New Key Form */}
      <div className="flex gap-3 mb-6">
        <input
          type="text"
          value={newKeyName}
          onChange={(e) => setNewKeyName(e.target.value)}
          placeholder="Key name (e.g., n8n automation)"
          className="flex-1 bg-bg-tertiary border border-glass-border rounded-lg px-4 py-2.5 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
        />
        <button
          onClick={handleCreateKey}
          disabled={isCreating}
          className="px-4 py-2.5 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors flex items-center gap-2"
        >
          {isCreating ? (
            <Loader2 className="w-4 h-4 animate-spin" />
          ) : (
            <Plus className="w-4 h-4" />
          )}
          Generate Key
        </button>
      </div>

      {/* New Key Display */}
      {newKey && (
        <div className="mb-6 p-4 bg-green-500/10 border border-green-500/30 rounded-lg">
          <p className="text-green-400 text-sm mb-2 font-medium">
            ⚠️ Save this key now! It won't be shown again.
          </p>
          <div className="flex items-center gap-2">
            <code className="flex-1 bg-bg-tertiary px-3 py-2 rounded text-text-primary text-sm font-mono overflow-x-auto">
              {newKey}
            </code>
            <button
              onClick={handleCopyKey}
              className="p-2 bg-bg-tertiary rounded hover:bg-background-hover transition-colors"
            >
              {copied ? (
                <Check className="w-4 h-4 text-green-400" />
              ) : (
                <Copy className="w-4 h-4 text-text-secondary" />
              )}
            </button>
          </div>
        </div>
      )}

      {/* Existing Keys */}
      <div className="space-y-3">
        <h3 className="text-sm font-medium text-text-secondary">Active Keys</h3>
        
        {isLoading ? (
          <div className="flex justify-center py-4">
            <Loader2 className="w-6 h-6 animate-spin text-accent-purple" />
          </div>
        ) : apiKeys && apiKeys.length > 0 ? (
          <div className="space-y-2">
            {apiKeys.map((key) => (
              <div
                key={key.id}
                className="flex items-center justify-between p-3 bg-bg-tertiary rounded-lg"
              >
                <div>
                  <p className="text-text-primary font-medium">{key.name}</p>
                  <p className="text-text-secondary text-sm">
                    {key.keyPrefix}••••••••
                    {key.lastUsedAt && ` • Last used: ${new Date(key.lastUsedAt).toLocaleDateString()}`}
                  </p>
                </div>
                <button
                  onClick={() => handleRevoke(key.id)}
                  className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                  title="Revoke key"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-text-secondary text-sm py-4">No API keys yet. Create one above.</p>
        )}
      </div>
    </GlassCard>
  );
}

export function DimensionSettings() {
  const { data: profile, isLoading } = useGetProfileQuery();

  if (isLoading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex justify-center py-8">
          <Loader2 className="w-6 h-6 animate-spin text-accent-purple" />
        </div>
      </GlassCard>
    );
  }

  return (
    <GlassCard variant="default" className="p-6">
      <h2 className="text-xl font-semibold text-text-primary mb-6">Dimension Settings</h2>
      <p className="text-text-secondary mb-6">
        Your 8 life dimensions and their current weights.
      </p>
      
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {profile?.dimensions.map((dim) => (
          <div
            key={dim.id}
            className="p-4 bg-bg-tertiary rounded-lg flex items-center gap-4"
          >
            <div className="text-2xl">{dim.icon || '📊'}</div>
            <div className="flex-1">
              <p className="text-text-primary font-medium">{dim.name}</p>
              <p className="text-text-secondary text-sm">{dim.code}</p>
            </div>
            <div className="text-accent-purple font-semibold">
              {Math.round(dim.weight * 100)}%
            </div>
          </div>
        ))}
      </div>
      
      <p className="text-text-secondary text-sm mt-6">
        Adjust dimension weights in the Profile settings tab.
      </p>
    </GlassCard>
  );
}

export function TaxProfileSettings() {
  const { data: taxProfiles, isLoading, error } = useGetTaxProfilesQuery();
  const [createTaxProfile, { isLoading: isCreating }] = useCreateTaxProfileMutation();
  const [updateTaxProfile] = useUpdateTaxProfileMutation();
  const [deleteTaxProfile] = useDeleteTaxProfileMutation();
  
  const [showForm, setShowForm] = useState(false);
  const [editingProfile, setEditingProfile] = useState<TaxProfile | null>(null);
  
  // Refs for auto-scrolling tax profile forms
  const taxProfileEditFormRef = useRef<HTMLDivElement>(null);
  const newTaxProfileFormRef = useRef<HTMLDivElement>(null);
  
  // Auto-scroll to tax profile edit form
  useEffect(() => {
    if (editingProfile && taxProfileEditFormRef.current) {
      setTimeout(() => {
        const element = taxProfileEditFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20;
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [editingProfile]);

  useEffect(() => {
    if (showForm && !editingProfile && newTaxProfileFormRef.current) {
      setTimeout(() => {
        const element = newTaxProfileFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20;
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [showForm, editingProfile]);

  const [formData, setFormData] = useState<CreateTaxProfileRequest>({
    name: '',
    taxYear: new Date().getFullYear(),
    countryCode: 'ZA',
    brackets: [],
    uifRate: 0.01,
    uifCap: 177.12, // Monthly contribution cap (R17,712 income ceiling × 1%)
    vatRate: 0.15,
    isVatRegistered: false,
    taxRebates: { primary: 17235, secondary: 9444, tertiary: 3145 },
  });

  // SA 2024/25 default brackets
  const defaultSABrackets = [
    { min: 0, max: 237100, rate: 0.18, baseTax: 0 },
    { min: 237101, max: 370500, rate: 0.26, baseTax: 42678 },
    { min: 370501, max: 512800, rate: 0.31, baseTax: 77362 },
    { min: 512801, max: 673000, rate: 0.36, baseTax: 121475 },
    { min: 673001, max: 857900, rate: 0.39, baseTax: 179147 },
    { min: 857901, max: 1817000, rate: 0.41, baseTax: 251258 },
    { min: 1817001, max: null, rate: 0.45, baseTax: 644489 },
  ];

  const handleLoadDefaults = () => {
    setFormData(prev => ({
      ...prev,
      name: 'South Africa 2024/25',
      taxYear: 2025,
      countryCode: 'ZA',
      brackets: defaultSABrackets,
      uifRate: 0.01,
      uifCap: 177.12, // Monthly contribution cap (R17,712 income ceiling × 1%)
      vatRate: 0.15,
      taxRebates: { primary: 17235, secondary: 9444, tertiary: 3145 },
    }));
  };

  const handleAddBracket = () => {
    const lastBracket = formData.brackets[formData.brackets.length - 1];
    const newMin = lastBracket ? (lastBracket.max || lastBracket.min) + 1 : 0;
    setFormData(prev => ({
      ...prev,
      brackets: [...prev.brackets, { min: newMin, max: null, rate: 0.18, baseTax: 0 }],
    }));
  };

  const handleRemoveBracket = (index: number) => {
    setFormData(prev => ({
      ...prev,
      brackets: prev.brackets.filter((_, i) => i !== index),
    }));
  };

  const handleBracketChange = (index: number, field: keyof TaxBracket, value: number | null) => {
    setFormData(prev => ({
      ...prev,
      brackets: prev.brackets.map((b, i) => 
        i === index ? { ...b, [field]: value } : b
      ),
    }));
  };

  const handleSubmit = async () => {
    try {
      if (editingProfile) {
        await updateTaxProfile({
          id: editingProfile.id,
          name: formData.name,
          brackets: formData.brackets,
          uifRate: formData.uifRate,
          uifCap: formData.uifCap,
          vatRate: formData.vatRate,
          isVatRegistered: formData.isVatRegistered,
          taxRebates: formData.taxRebates,
        }).unwrap();
      } else {
        await createTaxProfile(formData).unwrap();
      }
      setShowForm(false);
      setEditingProfile(null);
      setFormData({
        name: '',
        taxYear: new Date().getFullYear(),
        countryCode: 'ZA',
        brackets: [],
        uifRate: 0.01,
        uifCap: 177.12, // Monthly contribution cap (R17,712 income ceiling × 1%)
        vatRate: 0.15,
        isVatRegistered: false,
        taxRebates: { primary: 17235, secondary: 9444, tertiary: 3145 },
      });
    } catch (err) {
      console.error('Failed to save tax profile:', err);
    }
  };

  const handleEdit = (profile: TaxProfile) => {
    setEditingProfile(profile);
    setFormData({
      name: profile.name,
      taxYear: profile.taxYear,
      countryCode: profile.countryCode,
      brackets: profile.brackets || [],
      uifRate: profile.uifRate,
      uifCap: profile.uifCap,
      vatRate: profile.vatRate,
      isVatRegistered: profile.isVatRegistered,
      taxRebates: profile.taxRebates,
    });
    setShowForm(true);
  };

  const handleDelete = async (id: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to delete this tax profile?',
    });
    if (confirmed) {
      await deleteTaxProfile(id);
    }
  };

  const handleSetActive = async (profile: TaxProfile) => {
    await updateTaxProfile({
      id: profile.id,
      isActive: true,
    });
  };

  if (isLoading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex justify-center py-8">
          <Loader2 className="w-6 h-6 animate-spin text-accent-purple" />
        </div>
      </GlassCard>
    );
  }

  if (error) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center gap-3 text-red-400">
          <AlertCircle className="w-5 h-5" />
          <span>Failed to load tax profiles</span>
        </div>
      </GlassCard>
    );
  }

  return (
    <GlassCard variant="default" className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-xl font-semibold text-text-primary">Tax Profiles</h2>
          <p className="text-text-secondary text-sm mt-1">
            Configure tax brackets and deductions for financial simulations.
          </p>
        </div>
        <button
          onClick={() => {
            setEditingProfile(null);
            setFormData({
              name: '',
              taxYear: new Date().getFullYear(),
              countryCode: 'ZA',
              brackets: [],
              uifRate: 0.01,
              uifCap: 177.12, // Monthly contribution cap (R17,712 income ceiling × 1%)
              vatRate: 0.15,
              isVatRegistered: false,
              taxRebates: { primary: 17235, secondary: 9444, tertiary: 3145 },
            });
            setShowForm(true);
          }}
          className="px-4 py-2 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors flex items-center gap-2"
        >
          <Plus className="w-4 h-4" />
          Add Profile
        </button>
      </div>

      {/* Form */}
      {showForm && (
        <div ref={editingProfile ? taxProfileEditFormRef : newTaxProfileFormRef} className="mb-6 p-4 bg-bg-tertiary rounded-lg border border-glass-border">
          <h3 className="text-lg font-medium text-text-primary mb-4">
            {editingProfile ? 'Edit Tax Profile' : 'New Tax Profile'}
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">Name</label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                placeholder="e.g., South Africa 2024/25"
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Tax Year
                  <InfoTooltip content={TOOLTIP_CONTENT.taxProfile.taxYear} />
                </span>
              </label>
              <input
                type="number"
                value={formData.taxYear}
                onChange={(e) => setFormData(prev => ({ ...prev, taxYear: parseInt(e.target.value) }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Country
                  <InfoTooltip content={TOOLTIP_CONTENT.taxProfile.country} />
                </span>
              </label>
              <select
                value={formData.countryCode}
                onChange={(e) => setFormData(prev => ({ ...prev, countryCode: e.target.value }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              >
                <option value="ZA">South Africa</option>
                <option value="US">United States</option>
                <option value="GB">United Kingdom</option>
              </select>
            </div>
            <div className="flex items-end">
              <button
                type="button"
                onClick={handleLoadDefaults}
                className="px-4 py-2 bg-blue-600 rounded-lg text-white text-sm hover:bg-blue-700 transition-colors"
              >
                Load SA 2024/25 Defaults
              </button>
            </div>
          </div>

          {/* Tax Brackets */}
          <div className="mb-4">
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium text-text-secondary">
                <span className="inline-flex items-center gap-1.5">
                  Tax Brackets
                  <InfoTooltip content={TOOLTIP_CONTENT.taxProfile.brackets} />
                </span>
              </label>
              <button
                type="button"
                onClick={handleAddBracket}
                className="text-accent-purple text-sm hover:underline flex items-center gap-1"
              >
                <Plus className="w-3 h-3" /> Add Bracket
              </button>
            </div>
            <div className="space-y-2">
              {formData.brackets.map((bracket, index) => (
                <div key={index} className="flex items-center gap-2 p-2 bg-background-primary rounded-lg">
                  <div className="flex-1 grid grid-cols-4 gap-2">
                    <div>
                      <label className="block text-xs text-text-secondary">Min</label>
                      <input
                        type="number"
                        value={bracket.min}
                        onChange={(e) => handleBracketChange(index, 'min', parseFloat(e.target.value))}
                        className="w-full bg-bg-tertiary border border-glass-border rounded px-2 py-1 text-text-primary text-sm [color-scheme:dark]"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-text-secondary">Max</label>
                      <input
                        type="number"
                        value={bracket.max || ''}
                        onChange={(e) => handleBracketChange(index, 'max', e.target.value ? parseFloat(e.target.value) : null)}
                        placeholder="∞"
                        className="w-full bg-bg-tertiary border border-glass-border rounded px-2 py-1 text-text-primary text-sm [color-scheme:dark]"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-text-secondary">Rate %</label>
                      <input
                        type="number"
                        step="0.01"
                        value={(bracket.rate * 100).toFixed(0)}
                        onChange={(e) => handleBracketChange(index, 'rate', parseFloat(e.target.value) / 100)}
                        className="w-full bg-bg-tertiary border border-glass-border rounded px-2 py-1 text-text-primary text-sm [color-scheme:dark]"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-text-secondary">Base Tax</label>
                      <input
                        type="number"
                        value={bracket.baseTax}
                        onChange={(e) => handleBracketChange(index, 'baseTax', parseFloat(e.target.value))}
                        className="w-full bg-bg-tertiary border border-glass-border rounded px-2 py-1 text-text-primary text-sm [color-scheme:dark]"
                      />
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleRemoveBracket(index)}
                    className="p-1 text-red-400 hover:bg-red-400/10 rounded"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              ))}
              {formData.brackets.length === 0 && (
                <p className="text-text-secondary text-sm py-2">No brackets defined. Add one or load defaults.</p>
              )}
            </div>
          </div>

          {/* Additional Settings */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  UIF Rate %
                  <InfoTooltip content={TOOLTIP_CONTENT.taxProfile.uifRate} />
                </span>
              </label>
              <input
                type="number"
                step="0.01"
                value={(formData.uifRate || 0) * 100}
                onChange={(e) => setFormData(prev => ({ ...prev, uifRate: parseFloat(e.target.value) / 100 }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  UIF Cap (Annual)
                  <InfoTooltip content={TOOLTIP_CONTENT.taxProfile.uifCap} />
                </span>
              </label>
              <input
                type="number"
                value={formData.uifCap || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, uifCap: parseFloat(e.target.value) }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  VAT Rate %
                  <InfoTooltip content={TOOLTIP_CONTENT.taxProfile.vatRate} />
                </span>
              </label>
              <input
                type="number"
                step="0.01"
                value={(formData.vatRate || 0) * 100}
                onChange={(e) => setFormData(prev => ({ ...prev, vatRate: parseFloat(e.target.value) / 100 }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
          </div>

          {/* Tax Rebates */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-text-secondary mb-2">
              <span className="inline-flex items-center gap-1.5">
                Tax Rebates (Annual)
                <InfoTooltip content={TOOLTIP_CONTENT.taxProfile.rebates} />
              </span>
            </label>
            <div className="grid grid-cols-3 gap-4">
              <div>
                <label className="block text-xs text-text-secondary mb-1">Primary (under 65)</label>
                <input
                  type="number"
                  value={formData.taxRebates?.primary || ''}
                  onChange={(e) => setFormData(prev => ({
                    ...prev,
                    taxRebates: { ...prev.taxRebates, primary: parseFloat(e.target.value) }
                  }))}
                  className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary text-sm focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                />
              </div>
              <div>
                <label className="block text-xs text-text-secondary mb-1">Secondary (65-74)</label>
                <input
                  type="number"
                  value={formData.taxRebates?.secondary || ''}
                  onChange={(e) => setFormData(prev => ({
                    ...prev,
                    taxRebates: { ...prev.taxRebates, secondary: parseFloat(e.target.value) }
                  }))}
                  className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary text-sm focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                />
              </div>
              <div>
                <label className="block text-xs text-text-secondary mb-1">Tertiary (75+)</label>
                <input
                  type="number"
                  value={formData.taxRebates?.tertiary || ''}
                  onChange={(e) => setFormData(prev => ({
                    ...prev,
                    taxRebates: { ...prev.taxRebates, tertiary: parseFloat(e.target.value) }
                  }))}
                  className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary text-sm focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                />
              </div>
            </div>
          </div>

          {/* VAT Registered */}
          <div className="mb-4">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={formData.isVatRegistered}
                onChange={(e) => setFormData(prev => ({ ...prev, isVatRegistered: e.target.checked }))}
                className="w-4 h-4 accent-accent-purple"
              />
              <span className="text-text-primary text-sm">VAT Registered</span>
            </label>
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-3">
            <button
              type="button"
              onClick={() => {
                setShowForm(false);
                setEditingProfile(null);
              }}
              className="px-4 py-2 bg-background-primary border border-glass-border rounded-lg text-text-secondary hover:text-text-primary transition-colors"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={handleSubmit}
              disabled={isCreating || !formData.name}
              className="px-4 py-2 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors disabled:opacity-50 flex items-center gap-2"
            >
              {isCreating ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  {editingProfile ? 'Update' : 'Create'} Profile
                </>
              )}
            </button>
          </div>
        </div>
      )}

      {/* Existing Profiles */}
      <div className="space-y-3">
        {taxProfiles && taxProfiles.length > 0 ? (
          taxProfiles.map((profile) => (
            <div
              key={profile.id}
              className={cn(
                "p-4 rounded-lg border transition-colors",
                profile.isActive
                  ? "bg-accent-purple/10 border-accent-purple/30"
                  : "bg-bg-tertiary border-glass-border"
              )}
            >
              <div className="flex items-start justify-between">
                <div>
                  <div className="flex items-center gap-2">
                    <h4 className="text-text-primary font-medium">{profile.name}</h4>
                    {profile.isActive && (
                      <span className="px-2 py-0.5 bg-accent-purple/20 text-accent-purple text-xs rounded-full">
                        Active
                      </span>
                    )}
                  </div>
                  <p className="text-text-secondary text-sm mt-1">
                    {profile.countryCode} • Tax Year {profile.taxYear} • {profile.brackets?.length || 0} brackets
                  </p>
                  {profile.brackets && profile.brackets.length > 0 && (
                    <div className="mt-2 text-xs text-text-secondary">
                      Rates: {profile.brackets.map(b => `${(b.rate * 100).toFixed(0)}%`).join(' → ')}
                    </div>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  {!profile.isActive && (
                    <button
                      onClick={() => handleSetActive(profile)}
                      className="px-3 py-1 text-sm bg-accent-purple/20 text-accent-purple rounded hover:bg-accent-purple/30 transition-colors"
                    >
                      Set Active
                    </button>
                  )}
                  <button
                    onClick={() => handleEdit(profile)}
                    className="p-2 text-text-secondary hover:text-text-primary hover:bg-background-hover rounded transition-colors"
                  >
                    <Calculator className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => handleDelete(profile.id)}
                    className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </div>
          ))
        ) : (
          <div className="text-center py-8 text-text-secondary">
            <Calculator className="w-12 h-12 mx-auto mb-3 opacity-50" />
            <p>No tax profiles configured yet.</p>
            <p className="text-sm">Create one to enable accurate tax calculations in simulations.</p>
          </div>
        )}
      </div>
    </GlassCard>
  );
}

export function IncomeExpenseSettings() {
  const { data: incomeData, isLoading: incomeLoading } = useGetIncomeSourcesWithSummaryQuery();
  const { data: expenseDefinitions, isLoading: expenseLoading } = useGetExpenseDefinitionsQuery();
  const { data: investmentData } = useGetInvestmentContributionsQuery();
  const { data: taxProfiles } = useGetTaxProfilesQuery();
  const { data: accountsData } = useGetAccountsQuery();
  
  const incomeSources = incomeData?.sources;
  const incomeSummary = incomeData?.summary;
  
  const [createIncomeSource, { isLoading: isCreatingIncome }] = useCreateIncomeSourceMutation();
  const [updateIncomeSource] = useUpdateIncomeSourceMutation();
  const [deleteIncomeSource] = useDeleteIncomeSourceMutation();
  
  const [createExpenseDefinition, { isLoading: isCreatingExpense }] = useCreateExpenseDefinitionMutation();
  const [updateExpenseDefinition] = useUpdateExpenseDefinitionMutation();
  const [deleteExpenseDefinition] = useDeleteExpenseDefinitionMutation();
  
  const [activeTab, setActiveTab] = useState<'income' | 'expenses'>('income');
  const [showIncomeForm, setShowIncomeForm] = useState(false);
  const [showExpenseForm, setShowExpenseForm] = useState(false);
  const [editingIncome, setEditingIncome] = useState<IncomeSource | null>(null);
  const [editingExpense, setEditingExpense] = useState<ExpenseDefinition | null>(null);
  
  // Sorting state - default to amount descending (largest first), persisted to localStorage
  type SortOption = 'amount-desc' | 'amount-asc' | 'name-asc' | 'name-desc';
  const [incomeSortBy, setIncomeSortBy] = useState<SortOption>(() => {
    const saved = localStorage.getItem('lifeos-income-sort');
    return (saved as SortOption) || 'amount-desc';
  });
  const [expenseSortBy, setExpenseSortBy] = useState<SortOption>(() => {
    const saved = localStorage.getItem('lifeos-expense-sort');
    return (saved as SortOption) || 'amount-desc';
  });
  
  // Persist sort preferences to localStorage
  useEffect(() => {
    localStorage.setItem('lifeos-income-sort', incomeSortBy);
  }, [incomeSortBy]);
  
  useEffect(() => {
    localStorage.setItem('lifeos-expense-sort', expenseSortBy);
  }, [expenseSortBy]);
  
  // Refs for auto-scrolling to edit forms
  const incomeEditFormRef = useRef<HTMLDivElement>(null);
  const expenseEditFormRef = useRef<HTMLDivElement>(null);
  const newIncomeFormRef = useRef<HTMLDivElement>(null);
  const newExpenseFormRef = useRef<HTMLDivElement>(null);
  
  // Refs for item cards (for scrolling after edit)
  const incomeCardRefs = useRef<Map<string, HTMLDivElement>>(new Map());
  const expenseCardRefs = useRef<Map<string, HTMLDivElement>>(new Map());
  
  // Track the last saved item ID for scroll-to-card behavior
  const [lastSavedIncomeId, setLastSavedIncomeId] = useState<string | null>(null);
  const [lastSavedExpenseId, setLastSavedExpenseId] = useState<string | null>(null);
  
  // Scroll to saved item card
  useEffect(() => {
    if (lastSavedIncomeId) {
      setTimeout(() => {
        const cardElement = incomeCardRefs.current.get(lastSavedIncomeId);
        if (cardElement) {
          cardElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        setLastSavedIncomeId(null);
      }, 150);
    }
  }, [lastSavedIncomeId]);
  
  useEffect(() => {
    if (lastSavedExpenseId) {
      setTimeout(() => {
        const cardElement = expenseCardRefs.current.get(lastSavedExpenseId);
        if (cardElement) {
          cardElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        setLastSavedExpenseId(null);
      }, 150);
    }
  }, [lastSavedExpenseId]);
  
  // Auto-scroll to edit form when opening
  useEffect(() => {
    if (editingIncome && incomeEditFormRef.current) {
      setTimeout(() => {
        const element = incomeEditFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20;
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [editingIncome]);

  useEffect(() => {
    if (editingExpense && expenseEditFormRef.current) {
      setTimeout(() => {
        const element = expenseEditFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20;
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [editingExpense]);

  useEffect(() => {
    if (showIncomeForm && !editingIncome && newIncomeFormRef.current) {
      setTimeout(() => {
        const element = newIncomeFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20;
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [showIncomeForm, editingIncome]);

  useEffect(() => {
    if (showExpenseForm && !editingExpense && newExpenseFormRef.current) {
      setTimeout(() => {
        const element = newExpenseFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20;
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [showExpenseForm, editingExpense]);

  // Income form state
  const [incomeForm, setIncomeForm] = useState<CreateIncomeSourceRequest>({
    name: '',
    currency: 'ZAR',
    baseAmount: 0,
    isPreTax: true,
    paymentFrequency: 'Monthly',
    employerName: '',
    notes: '',
    targetAccountId: undefined,
  });
  
  // Expense form state
  const [expenseForm, setExpenseForm] = useState<CreateExpenseDefinitionRequest>({
    name: '',
    currency: 'ZAR',
    amountType: 'Fixed',
    amountValue: 0,
    frequency: 'Monthly',
    category: 'Other',
    isTaxDeductible: false,
    inflationAdjusted: true,
    endConditionType: 'None',
  });

  // State for interactive pie chart
  const [activePieIndex, setActivePieIndex] = useState<number | undefined>(undefined);
  const isMobile = useBreakpoint('sm');

  const frequencyOptions = [
    { value: 'Monthly', label: 'Monthly' },
    { value: 'Annually', label: 'Annually' },
    { value: 'Weekly', label: 'Weekly' },
    { value: 'Biweekly', label: 'Bi-Weekly' },
    { value: 'Once', label: 'One-Time' },
  ];

  const endConditionOptions = [
    { value: 'None', label: 'No end condition (runs indefinitely)' },
    { value: 'UntilAccountSettled', label: 'Until account is settled (balance = 0)' },
    { value: 'UntilDate', label: 'Until a specific date' },
    { value: 'UntilAmount', label: 'Until total amount paid' },
  ];

  const expenseCategories = [
    'Housing', 'Transport', 'Food', 'Utilities', 'Insurance', 
    'Healthcare', 'Entertainment', 'Education', 'Savings', 'Debt', 'Subscriptions', 
    'Communications', 'Personal Care', 'Pets', 'Household', 'Other'
  ];

  const resetIncomeForm = () => {
    setIncomeForm({
      name: '',
      currency: 'ZAR',
      baseAmount: 0,
      isPreTax: true,
      paymentFrequency: 'Monthly',
      employerName: '',
      notes: '',
      targetAccountId: undefined,
    });
    setEditingIncome(null);
    setShowIncomeForm(false);
  };

  const resetExpenseForm = () => {
    setExpenseForm({
      name: '',
      currency: 'ZAR',
      amountType: 'Fixed',
      amountValue: 0,
      frequency: 'Monthly',
      startDate: undefined,
      category: 'Other',
      isTaxDeductible: false,
      inflationAdjusted: true,
      linkedAccountId: undefined,
      endConditionType: 'None',
      endConditionAccountId: undefined,
      endDate: undefined,
      endAmountThreshold: undefined,
    });
    setEditingExpense(null);
    setShowExpenseForm(false);
  };

  const handleEditIncome = (income: IncomeSource) => {
    setEditingIncome(income);
    // Convert paymentFrequency from lowercase API response to PascalCase for form
    const frequencyMap: Record<string, CreateIncomeSourceRequest['paymentFrequency']> = {
      'monthly': 'Monthly',
      'annual': 'Annually',
      'annually': 'Annually',
      'weekly': 'Weekly',
      'biweekly': 'Biweekly',
      'once': 'Once',
    };
    setIncomeForm({
      name: income.name,
      currency: income.currency,
      baseAmount: income.baseAmount,
      isPreTax: income.isPreTax,
      taxProfileId: income.taxProfileId,
      paymentFrequency: frequencyMap[income.paymentFrequency?.toLowerCase() || 'monthly'] || 'Monthly',
      nextPaymentDate: income.nextPaymentDate ? income.nextPaymentDate.split('T')[0] : undefined,
      annualIncreaseRate: income.annualIncreaseRate,
      employerName: income.employerName || '',
      notes: income.notes || '',
      targetAccountId: income.targetAccountId,
    });
    setShowIncomeForm(true);
  };

  const handleEditExpense = (expense: ExpenseDefinition) => {
    setEditingExpense(expense);
    // Convert frequency from lowercase API response to PascalCase for form
    const frequencyMap: Record<string, CreateExpenseDefinitionRequest['frequency']> = {
      'monthly': 'Monthly',
      'annual': 'Annually',
      'annually': 'Annually',
      'weekly': 'Weekly',
      'biweekly': 'Biweekly',
      'once': 'Once',
    };
    // Convert endConditionType from lowercase API response to PascalCase for form
    const endConditionTypeMap: Record<string, CreateExpenseDefinitionRequest['endConditionType']> = {
      'none': 'None',
      'untilaccountsettled': 'UntilAccountSettled',
      'untildate': 'UntilDate',
      'untilamount': 'UntilAmount',
    };
    setExpenseForm({
      name: expense.name,
      currency: expense.currency,
      amountType: expense.amountType,
      amountValue: expense.amountValue,
      amountFormula: expense.amountFormula,
      frequency: frequencyMap[expense.frequency?.toLowerCase() || 'monthly'] || 'Monthly',
      startDate: expense.startDate,
      category: expense.category,
      isTaxDeductible: expense.isTaxDeductible,
      inflationAdjusted: expense.inflationAdjusted,
      linkedAccountId: expense.linkedAccountId,
      endConditionType: endConditionTypeMap[expense.endConditionType?.toLowerCase() || 'none'] || 'None',
      endConditionAccountId: expense.endConditionAccountId,
      endDate: expense.endDate,
      endAmountThreshold: expense.endAmountThreshold,
    });
    setShowExpenseForm(true);
  };

  const handleSubmitIncome = async () => {
    try {
      const savedId = editingIncome?.id;
      if (editingIncome) {
        const clearTaxProfile = !incomeForm.taxProfileId && editingIncome.taxProfileId ? true : false;
        await updateIncomeSource({
          id: editingIncome.id,
          ...incomeForm,
          clearTaxProfile,
        }).unwrap();
      } else {
        await createIncomeSource(incomeForm as CreateIncomeSourceRequest).unwrap();
      }
      resetIncomeForm();
      // Scroll to the saved item card after edit
      if (savedId) {
        setLastSavedIncomeId(savedId);
      }
    } catch (err) {
      console.error('Failed to save income source:', err);
    }
  };

  const handleSubmitExpense = async () => {
    try {
      const savedId = editingExpense?.id;
      if (editingExpense) {
        await updateExpenseDefinition({
          id: editingExpense.id,
          ...expenseForm,
        }).unwrap();
      } else {
        await createExpenseDefinition(expenseForm as CreateExpenseDefinitionRequest).unwrap();
      }
      resetExpenseForm();
      // Scroll to the saved item card after edit
      if (savedId) {
        setLastSavedExpenseId(savedId);
      }
    } catch (err) {
      console.error('Failed to save expense definition:', err);
    }
  };

  const handleDeleteIncome = async (id: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to delete this income source?',
    });
    if (confirmed) {
      await deleteIncomeSource(id);
    }
  };

  const handleDeleteExpense = async (id: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to delete this expense?',
    });
    if (confirmed) {
      await deleteExpenseDefinition(id);
    }
  };

  const formatCurrency = (amount: number, currency: string = 'ZAR') => {
    const symbols: Record<string, string> = { ZAR: 'R', USD: '$', EUR: '€', GBP: '£' };
    return `${symbols[currency] || currency} ${amount.toLocaleString()}`;
  };

  // Check if a date falls within the current month
  const isInCurrentMonth = (dateStr?: string): boolean => {
    if (!dateStr) return false;
    const date = new Date(dateStr);
    const now = new Date();
    return date.getFullYear() === now.getFullYear() && date.getMonth() === now.getMonth();
  };
  
  // Check if a scheduled date is in the past (for one-time payments)
  const isScheduledDatePast = (dateStr?: string): boolean => {
    if (!dateStr) return false;
    const date = new Date(dateStr);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return date < today;
  };

  const getMonthlyAmount = (amount: number, frequency: string, startDate?: string) => {
    const freq = frequency.toLowerCase();
    switch (freq) {
      case 'annual':
      case 'annually': return amount / 12;
      case 'weekly': return amount * 4.33;
      case 'biweekly': return amount * 2.17;
      case 'once': 
        // Once-off expenses in the current month are included at full value
        return isInCurrentMonth(startDate) ? amount : 0;
      case 'quarterly': return amount / 3;
      default: return amount; // monthly
    }
  };

  // For sorting purposes, we want to sort one-time items by their face value
  const getSortableAmount = (amount: number, frequency: string) => {
    const freq = frequency?.toLowerCase() || 'monthly';
    if (freq === 'once') return amount; // Sort one-time items by face value
    return getMonthlyAmount(amount, frequency);
  };

  // Sort income sources
  const sortedIncomeSources = [...(incomeSources || [])].sort((a, b) => {
    switch (incomeSortBy) {
      case 'amount-desc':
        return getSortableAmount(b.baseAmount, b.paymentFrequency) - getSortableAmount(a.baseAmount, a.paymentFrequency);
      case 'amount-asc':
        return getSortableAmount(a.baseAmount, a.paymentFrequency) - getSortableAmount(b.baseAmount, b.paymentFrequency);
      case 'name-asc':
        return a.name.localeCompare(b.name);
      case 'name-desc':
        return b.name.localeCompare(a.name);
      default:
        return 0;
    }
  });

  // Sort expense definitions
  const sortedExpenseDefinitions = [...(expenseDefinitions || [])].sort((a, b) => {
    const amountA = a.amountValue || 0;
    const amountB = b.amountValue || 0;
    switch (expenseSortBy) {
      case 'amount-desc':
        return getSortableAmount(amountB, b.frequency) - getSortableAmount(amountA, a.frequency);
      case 'amount-asc':
        return getSortableAmount(amountA, a.frequency) - getSortableAmount(amountB, b.frequency);
      case 'name-asc':
        return a.name.localeCompare(b.name);
      case 'name-desc':
        return b.name.localeCompare(a.name);
      default:
        return 0;
    }
  });

  // Use API-calculated values when available, fallback to local calculation
  const totalMonthlyGross = incomeSummary?.totalMonthlyGross ?? (incomeSources?.filter(i => i.isActive).reduce(
    (sum, i) => sum + getMonthlyAmount(i.baseAmount, i.paymentFrequency), 0
  ) || 0);
  
  const totalMonthlyTax = incomeSummary?.totalMonthlyTax || 0;
  const totalMonthlyUif = incomeSummary?.totalMonthlyUif || 0;
  const totalMonthlyNet = incomeSummary?.totalMonthlyNet ?? (totalMonthlyGross - totalMonthlyTax - totalMonthlyUif);

  const totalMonthlyExpenses = expenseDefinitions?.filter(e => e.isActive).reduce(
    (sum, e) => sum + getMonthlyAmount(e.amountValue || 0, e.frequency, e.startDate), 0
  ) || 0;
  
  // Investment contributions: regular monthly + once-off in current month
  const regularMonthlyInvestments = investmentData?.summary?.totalMonthlyContributions || 0;
  const onceOffInvestmentsThisMonth = (investmentData?.sources || [])
    .filter(inv => inv.isActive && inv.frequency.toLowerCase() === 'once' && isInCurrentMonth(inv.startDate))
    .reduce((sum, inv) => sum + inv.amount, 0);
  const totalMonthlyInvestments = regularMonthlyInvestments + onceOffInvestmentsThisMonth;
  
  // Get auto-calculated interest and fees from liability accounts
  const totalMonthlyInterest = accountsData?.meta?.totalMonthlyInterest || 0;
  const totalMonthlyFees = accountsData?.meta?.totalMonthlyFees || 0;
  
  const netCashFlow = totalMonthlyNet - totalMonthlyExpenses - totalMonthlyInterest - totalMonthlyFees;

  // Pie chart data for financial breakdown
  const pieChartData = [
    { name: 'Expenses', value: totalMonthlyExpenses, color: '#ef4444' },
    { name: 'Interest', value: totalMonthlyInterest, color: '#dc2626' },
    { name: 'Account Fees', value: totalMonthlyFees, color: '#b91c1c' },
    { name: 'Investments', value: totalMonthlyInvestments, color: '#8b5cf6' },
    { name: 'Tax (PAYE)', value: totalMonthlyTax, color: '#f97316' },
    { name: 'UIF', value: totalMonthlyUif, color: '#eab308' },
  ].filter(item => item.value > 0);

  const totalAllocated = totalMonthlyExpenses + totalMonthlyInterest + totalMonthlyFees + totalMonthlyInvestments + totalMonthlyTax + totalMonthlyUif;
  const unallocated = Math.max(0, totalMonthlyGross - totalAllocated);
  if (unallocated > 0) {
    pieChartData.push({ name: 'Unallocated', value: unallocated, color: '#22c55e' });
  }
  
  // Category colors for expense breakdown pie chart
  const categoryColors: Record<string, string> = {
    'Housing': '#8b5cf6',
    'Transport': '#3b82f6',
    'Food': '#22c55e',
    'Utilities': '#eab308',
    'Insurance': '#f97316',
    'Healthcare': '#ef4444',
    'Entertainment': '#ec4899',
    'Education': '#06b6d4',
    'Savings': '#10b981',
    'Debt': '#dc2626',
    'Subscriptions': '#a855f7',
    'Communications': '#6366f1',
    'Personal Care': '#f472b6',
    'Pets': '#84cc16',
    'Household': '#14b8a6',
    'Other': '#6b7280',
  };
  
  // Calculate expense breakdown by category (for active recurring + current month one-time)
  const expenseCategoryBreakdown = (expenseDefinitions || [])
    .filter(e => e.isActive)
    .reduce((acc, expense) => {
      const monthlyAmount = getMonthlyAmount(expense.amountValue || 0, expense.frequency, expense.startDate);
      if (monthlyAmount > 0) {
        acc[expense.category] = (acc[expense.category] || 0) + monthlyAmount;
      }
      return acc;
    }, {} as Record<string, number>);
  
  const expenseCategoryPieData = Object.entries(expenseCategoryBreakdown)
    .map(([category, value]) => ({
      name: category,
      value,
      color: categoryColors[category] || '#6b7280',
    }))
    .filter(item => item.value > 0)
    .sort((a, b) => b.value - a.value);
  
  // Separate active vs completed one-time expenses
  const activeExpenses = sortedExpenseDefinitions.filter(e => {
    if (e.frequency.toLowerCase() !== 'once') return true;
    return !isScheduledDatePast(e.startDate);
  });
  
  const completedOneTimeExpenses = sortedExpenseDefinitions.filter(e => {
    return e.frequency.toLowerCase() === 'once' && isScheduledDatePast(e.startDate);
  });
  
  // Similarly for income
  const activeIncome = sortedIncomeSources.filter(i => {
    if (i.paymentFrequency.toLowerCase() !== 'once') return true;
    return !isScheduledDatePast(i.nextPaymentDate);
  });
  
  const completedOneTimeIncome = sortedIncomeSources.filter(i => {
    return i.paymentFrequency.toLowerCase() === 'once' && isScheduledDatePast(i.nextPaymentDate);
  });

  const isLoading = incomeLoading || expenseLoading;

  if (isLoading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex justify-center py-8">
          <Loader2 className="w-6 h-6 animate-spin text-accent-purple" />
        </div>
      </GlassCard>
    );
  }

  return (
    <GlassCard variant="default" className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-xl font-semibold text-text-primary">Income & Expenses</h2>
          <p className="text-text-secondary text-sm mt-1">
            Configure recurring income and expenses for financial simulations.
          </p>
        </div>
        <div className="text-right">
          <div className="text-sm text-text-secondary">Net Monthly Cash Flow</div>
          <div className={cn(
            "text-xl font-semibold",
            netCashFlow >= 0 ? "text-green-400" : "text-red-400"
          )}>
            {formatCurrency(netCashFlow)}
          </div>
        </div>
      </div>
      
      {/* Tax Breakdown Summary */}
      {(totalMonthlyTax > 0 || totalMonthlyUif > 0) && (
        <div className="mb-6 p-4 bg-bg-tertiary rounded-lg border border-glass-border">
          <h3 className="text-sm font-medium text-text-primary mb-3">Monthly Income Breakdown</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <div className="text-text-secondary">Gross Income</div>
              <div className="text-green-400 font-medium">{formatCurrency(totalMonthlyGross)}</div>
            </div>
            <div>
              <div className="text-text-secondary">PAYE Tax</div>
              <div className="text-red-400 font-medium">-{formatCurrency(totalMonthlyTax)}</div>
            </div>
            <div>
              <div className="text-text-secondary">UIF</div>
              <div className="text-red-400 font-medium">-{formatCurrency(totalMonthlyUif)}</div>
            </div>
            <div>
              <div className="text-text-secondary">Net Income</div>
              <div className="text-accent-purple font-medium">{formatCurrency(totalMonthlyNet)}</div>
            </div>
          </div>
          {(totalMonthlyInterest > 0 || totalMonthlyFees > 0) && (
            <div className="mt-3 pt-3 border-t border-glass-border/50 space-y-2">
              {totalMonthlyInterest > 0 && (
                <div className="flex items-center justify-between text-sm">
                  <div className="text-text-secondary">Auto-calculated Interest (from liability accounts)</div>
                  <div className="text-red-400 font-medium">-{formatCurrency(totalMonthlyInterest)}</div>
                </div>
              )}
              {totalMonthlyFees > 0 && (
                <div className="flex items-center justify-between text-sm">
                  <div className="text-text-secondary">Account Fees (from all accounts)</div>
                  <div className="text-red-400 font-medium">-{formatCurrency(totalMonthlyFees)}</div>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* Financial Breakdown Pie Chart */}
      {pieChartData.length > 0 && totalMonthlyGross > 0 && (
        <div className="mb-6 p-3 sm:p-4 bg-bg-tertiary rounded-lg border border-glass-border overflow-hidden">
          <h3 className="text-xs sm:text-sm font-medium text-text-primary mb-3">Monthly Financial Breakdown</h3>
          <div className="flex flex-col lg:flex-row items-center gap-4">
            <div className="w-full lg:w-2/5 h-36 sm:h-60 lg:h-72 relative flex-shrink-0 min-w-[160px]">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <defs>
                    {pieChartData.map((entry, index) => (
                      <linearGradient key={`gradient-${index}`} id={`pieGradient-${index}`} x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor={entry.color} stopOpacity={1} />
                        <stop offset="100%" stopColor={entry.color} stopOpacity={0.7} />
                      </linearGradient>
                    ))}
                    <filter id="pieShadow" x="-20%" y="-20%" width="140%" height="140%">
                      <feDropShadow dx="0" dy="2" stdDeviation="3" floodColor="#000" floodOpacity="0.3" />
                    </filter>
                  </defs>
                  <Pie
                    data={pieChartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={isMobile ? "48%" : "50%"}
                    outerRadius={activePieIndex !== undefined ? "92%" : "88%"}
                    paddingAngle={2}
                    dataKey="value"
                    cornerRadius={3}
                    onMouseEnter={(_, index) => setActivePieIndex(index)}
                    onMouseLeave={() => setActivePieIndex(undefined)}
                  >
                    {pieChartData.map((_, index) => (
                      <Cell 
                        key={`cell-${index}`} 
                        fill={`url(#pieGradient-${index})`}
                        stroke="transparent"
                        strokeWidth={0}
                        style={{ 
                          cursor: 'pointer',
                          transition: 'all 0.3s ease',
                          filter: activePieIndex === index ? 'brightness(1.2) drop-shadow(0 4px 12px rgba(0, 0, 0, 0.4))' : 'none',
                          transform: activePieIndex === index ? 'scale(1.05)' : 'scale(1)',
                          transformOrigin: 'center'
                        }}
                      />
                    ))}
                  </Pie>
                  {/* Center text only on mobile */}
                  {isMobile && (
                    <text x="50%" y="50%" textAnchor="middle" dominantBaseline="central">
                      {activePieIndex !== undefined ? (
                        <>
                          <tspan x="50%" dy="-0.3em" fill="#fff" fontSize="9" fontWeight="500">
                            {pieChartData[activePieIndex]?.name}
                          </tspan>
                          <tspan x="50%" dy="1.1em" fill="#9ca3af" fontSize="8">
                            {((pieChartData[activePieIndex]?.value / totalMonthlyGross) * 100).toFixed(1)}%
                          </tspan>
                        </>
                      ) : (
                        <>
                          <tspan x="50%" dy="-0.3em" fill="#fff" fontSize="9" fontWeight="600">
                            {formatCurrency(totalMonthlyGross)}
                          </tspan>
                          <tspan x="50%" dy="1.1em" fill="#9ca3af" fontSize="8">
                            Total Gross
                          </tspan>
                        </>
                      )}
                    </text>
                  )}
                  <Tooltip
                    content={({ active, payload }) => {
                      if (!active || !payload?.length) return null;
                      const item = payload[0].payload;
                      const percent = ((item.value / totalMonthlyGross) * 100).toFixed(1);
                      return (
                        <div className="bg-bg-primary/95 backdrop-blur-sm border border-glass-border rounded-xl p-4 shadow-2xl">
                          <div className="flex items-center gap-2 mb-2">
                            <div 
                              className="w-3 h-3 rounded-full shadow-lg" 
                              style={{ backgroundColor: item.color }}
                            />
                            <p className="text-text-primary font-semibold">{item.name}</p>
                          </div>
                          <p className="text-xl font-bold text-white">{formatCurrency(item.value)}</p>
                          <p className="text-text-tertiary text-sm">{percent}% of gross income</p>
                        </div>
                      );
                    }}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="flex-1 space-y-1.5 min-w-0">
              {pieChartData.map((item, index) => (
                <div 
                  key={index} 
                  className={cn(
                    "flex items-center justify-between p-1.5 rounded-lg transition-all duration-200 cursor-pointer",
                    activePieIndex === index 
                      ? "bg-white/10 scale-[1.02]" 
                      : "hover:bg-white/5"
                  )}
                  onMouseEnter={() => setActivePieIndex(index)}
                  onMouseLeave={() => setActivePieIndex(undefined)}
                >
                  <div className="flex items-center gap-2 min-w-0">
                    <div 
                      className={cn(
                        "w-2.5 h-2.5 rounded-full transition-transform duration-200 flex-shrink-0",
                        activePieIndex === index && "scale-125"
                      )}
                      style={{ 
                        backgroundColor: item.color,
                        boxShadow: activePieIndex === index ? `0 0 8px ${item.color}` : 'none'
                      }}
                    />
                    <span className="text-text-primary text-xs truncate">{item.name}</span>
                  </div>
                  <div className="text-right flex-shrink-0 ml-2">
                    <span className={cn(
                      "text-xs transition-colors duration-200",
                      activePieIndex === index ? "text-white font-medium" : "text-text-secondary"
                    )}>
                      {formatCurrency(item.value)}
                    </span>
                    <span className="text-text-tertiary text-xs ml-1">
                      ({((item.value / totalMonthlyGross) * 100).toFixed(1)}%)
                    </span>
                  </div>
                </div>
              ))}
              <div className="border-t border-glass-border pt-2 mt-2">
                <div className="flex items-center justify-between font-medium px-1.5">
                  <span className="text-text-primary text-xs">Total Gross</span>
                  <span className="text-green-400 text-sm">{formatCurrency(totalMonthlyGross)}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tabs */}
      <div className="flex gap-2 mb-6 border-b border-glass-border">
        <button
          onClick={() => setActiveTab('income')}
          className={cn(
            "px-4 py-2 font-medium transition-colors border-b-2 -mb-px",
            activeTab === 'income'
              ? "text-green-400 border-green-400"
              : "text-text-secondary border-transparent hover:text-text-primary"
          )}
        >
          Income Sources ({incomeSources?.length || 0})
        </button>
        <button
          onClick={() => setActiveTab('expenses')}
          className={cn(
            "px-4 py-2 font-medium transition-colors border-b-2 -mb-px",
            activeTab === 'expenses'
              ? "text-red-400 border-red-400"
              : "text-text-secondary border-transparent hover:text-text-primary"
          )}
        >
          Expenses ({expenseDefinitions?.length || 0})
        </button>
      </div>

      {/* Income Tab */}
      {activeTab === 'income' && (
        <div className="space-y-4">
          <div className="flex justify-between items-center">
            <div className="text-sm text-text-secondary">
              Total Gross: <span className="text-green-400 font-medium">{formatCurrency(totalMonthlyGross)}</span>
              {totalMonthlyTax > 0 && (
                <span className="ml-3">
                  Net (after tax): <span className="text-accent-purple font-medium">{formatCurrency(totalMonthlyNet)}</span>
                </span>
              )}
            </div>
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2">
                <ArrowUpDown className="w-4 h-4 text-text-secondary" />
                <select
                  value={incomeSortBy}
                  onChange={(e) => setIncomeSortBy(e.target.value as SortOption)}
                  className="bg-bg-tertiary border border-glass-border rounded-lg px-2 py-1 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                >
                  <option value="amount-desc">Amount (High to Low)</option>
                  <option value="amount-asc">Amount (Low to High)</option>
                  <option value="name-asc">Name (A-Z)</option>
                  <option value="name-desc">Name (Z-A)</option>
                </select>
              </div>
              <button
                onClick={() => { resetIncomeForm(); setShowIncomeForm(true); }}
                className="px-4 py-2 bg-green-600 rounded-lg text-white font-medium hover:bg-green-700 transition-colors flex items-center gap-2"
              >
                <Plus className="w-4 h-4" />
                Add Income
              </button>
            </div>
          </div>

          {/* Income Form - Only shown when adding NEW (not editing) */}
          {showIncomeForm && !editingIncome && (
            <div ref={newIncomeFormRef} className="p-4 bg-bg-tertiary rounded-lg border border-glass-border">
              <h3 className="text-lg font-medium text-text-primary mb-4">
                New Income Source
              </h3>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">Name *</label>
                  <input
                    type="text"
                    value={incomeForm.name}
                    onChange={(e) => setIncomeForm(prev => ({ ...prev, name: e.target.value }))}
                    placeholder="e.g., Salary, Freelance, Rental Income"
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">Employer/Source</label>
                  <input
                    type="text"
                    value={incomeForm.employerName || ''}
                    onChange={(e) => setIncomeForm(prev => ({ ...prev, employerName: e.target.value }))}
                    placeholder="e.g., Company Name"
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">
                    <span className="inline-flex items-center gap-1.5">
                      Amount *
                      <InfoTooltip content={TOOLTIP_CONTENT.income.baseAmount} />
                    </span>
                  </label>
                  <div className="flex gap-2">
                    <select
                      value={incomeForm.currency}
                      onChange={(e) => setIncomeForm(prev => ({ ...prev, currency: e.target.value }))}
                      className="bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    >
                      <option value="ZAR">ZAR</option>
                      <option value="USD">USD</option>
                      <option value="EUR">EUR</option>
                      <option value="GBP">GBP</option>
                    </select>
                    <input
                      type="number"
                      value={incomeForm.baseAmount || ''}
                      onChange={(e) => setIncomeForm(prev => ({ ...prev, baseAmount: parseFloat(e.target.value) || 0 }))}
                      placeholder="0.00"
                      className="flex-1 bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">
                    <span className="inline-flex items-center gap-1.5">
                      Frequency
                      <InfoTooltip content={TOOLTIP_CONTENT.frequency.general} />
                    </span>
                  </label>
                  <select
                    value={incomeForm.paymentFrequency}
                    onChange={(e) => setIncomeForm(prev => ({ ...prev, paymentFrequency: e.target.value as PaymentFrequency }))}
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  >
                    {frequencyOptions.map(opt => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
                </div>
                {/* Scheduled Date for One-Time Income */}
                {incomeForm.paymentFrequency === 'Once' && (
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Scheduled Date *
                        <InfoTooltip content="The date when this one-time income will be received" />
                      </span>
                    </label>
                    <input
                      type="date"
                      value={incomeForm.nextPaymentDate || ''}
                      onChange={(e) => setIncomeForm(prev => ({ ...prev, nextPaymentDate: e.target.value || undefined }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                )}
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">
                    <span className="inline-flex items-center gap-1.5">
                      Tax Profile
                      <InfoTooltip content={TOOLTIP_CONTENT.taxProfile.selector} />
                    </span>
                  </label>
                  <select
                    value={incomeForm.taxProfileId || ''}
                    onChange={(e) => setIncomeForm(prev => ({ ...prev, taxProfileId: e.target.value || undefined }))}
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  >
                    <option value="">No Tax Profile</option>
                    {taxProfiles?.map(tp => (
                      <option key={tp.id} value={tp.id}>{tp.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">
                    <span className="inline-flex items-center gap-1.5">
                      Annual Increase %
                      <InfoTooltip content={TOOLTIP_CONTENT.income.annualIncrease} />
                    </span>
                  </label>
                  <input
                    type="number"
                    step="0.1"
                    value={incomeForm.annualIncreaseRate ? incomeForm.annualIncreaseRate * 100 : ''}
                    onChange={(e) => setIncomeForm(prev => ({ 
                      ...prev, 
                      annualIncreaseRate: e.target.value ? parseFloat(e.target.value) / 100 : undefined 
                    }))}
                    placeholder="e.g., 5 for 5%"
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">
                    <span className="inline-flex items-center gap-1.5">
                      Target Account
                      <InfoTooltip content="The account where this income will be deposited. Used in simulations to track cash flows." />
                    </span>
                  </label>
                  <select
                    value={incomeForm.targetAccountId || ''}
                    onChange={(e) => setIncomeForm(prev => ({ ...prev, targetAccountId: e.target.value || undefined }))}
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  >
                    <option value="">Default (first bank account)</option>
                    {accountsData?.data?.filter(acc => !acc.isLiability).map(acc => (
                      <option key={acc.id} value={acc.id}>{acc.name}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="mb-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={incomeForm.isPreTax}
                    onChange={(e) => setIncomeForm(prev => ({ ...prev, isPreTax: e.target.checked }))}
                    className="w-4 h-4 accent-green-500"
                  />
                  <span className="text-text-primary text-sm inline-flex items-center gap-1.5">
                    Amount is before tax (gross)
                    <InfoTooltip content={TOOLTIP_CONTENT.income.isPreTax} />
                  </span>
                </label>
              </div>

              <div className="mb-4">
                <label className="block text-sm font-medium text-text-secondary mb-1">Notes</label>
                <textarea
                  value={incomeForm.notes || ''}
                  onChange={(e) => setIncomeForm(prev => ({ ...prev, notes: e.target.value }))}
                  placeholder="Additional details..."
                  rows={2}
                  className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                />
              </div>

              <div className="flex justify-end gap-3">
                <button
                  onClick={resetIncomeForm}
                  className="px-4 py-2 bg-background-primary border border-glass-border rounded-lg text-text-secondary hover:text-text-primary transition-colors"
                >
                  Cancel
                </button>
                <button
                  onClick={handleSubmitIncome}
                  disabled={isCreatingIncome || !incomeForm.name || !incomeForm.baseAmount || !incomeForm.targetAccountId}
                  className="px-4 py-2 bg-green-600 rounded-lg text-white font-medium hover:bg-green-700 transition-colors disabled:opacity-50 flex items-center gap-2"
                >
                  {isCreatingIncome ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      Saving...
                    </>
                  ) : (
                    <>
                      <Check className="w-4 h-4" />
                      Create
                    </>
                  )}
                </button>
              </div>
            </div>
          )}

          {/* Income List */}
          <div className="space-y-2">
            {activeIncome && activeIncome.length > 0 ? (
              activeIncome.map((income) => (
                editingIncome?.id === income.id ? (
                  /* Inline Edit Form */
                  <div key={income.id} ref={incomeEditFormRef} className="p-4 bg-bg-tertiary rounded-lg border border-accent-purple">
                    <h3 className="text-lg font-medium text-text-primary mb-4">
                      Edit Income Source
                    </h3>
                    
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">Name *</label>
                        <input
                          type="text"
                          value={incomeForm.name}
                          onChange={(e) => setIncomeForm(prev => ({ ...prev, name: e.target.value }))}
                          placeholder="e.g., Salary, Freelance, Rental Income"
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">Employer/Source</label>
                        <input
                          type="text"
                          value={incomeForm.employerName || ''}
                          onChange={(e) => setIncomeForm(prev => ({ ...prev, employerName: e.target.value }))}
                          placeholder="e.g., Company Name"
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">
                          <span className="inline-flex items-center gap-1.5">
                            Amount *
                            <InfoTooltip content={TOOLTIP_CONTENT.income.baseAmount} />
                          </span>
                        </label>
                        <div className="flex gap-2">
                          <select
                            value={incomeForm.currency}
                            onChange={(e) => setIncomeForm(prev => ({ ...prev, currency: e.target.value }))}
                            className="bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          >
                            <option value="ZAR">ZAR</option>
                            <option value="USD">USD</option>
                            <option value="EUR">EUR</option>
                            <option value="GBP">GBP</option>
                          </select>
                          <input
                            type="number"
                            value={incomeForm.baseAmount || ''}
                            onChange={(e) => setIncomeForm(prev => ({ ...prev, baseAmount: parseFloat(e.target.value) || 0 }))}
                            placeholder="0.00"
                            className="flex-1 bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          />
                        </div>
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">
                          <span className="inline-flex items-center gap-1.5">
                            Frequency
                            <InfoTooltip content={TOOLTIP_CONTENT.frequency.general} />
                          </span>
                        </label>
                        <select
                          value={incomeForm.paymentFrequency}
                          onChange={(e) => setIncomeForm(prev => ({ ...prev, paymentFrequency: e.target.value as PaymentFrequency }))}
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        >
                          {frequencyOptions.map(opt => (
                            <option key={opt.value} value={opt.value}>{opt.label}</option>
                          ))}
                        </select>
                      </div>
                      {/* Scheduled Date for One-Time Income (inline edit) */}
                      {incomeForm.paymentFrequency === 'Once' && (
                        <div>
                          <label className="block text-sm font-medium text-text-secondary mb-1">
                            <span className="inline-flex items-center gap-1.5">
                              Scheduled Date *
                              <InfoTooltip content="The date when this one-time income will be received" />
                            </span>
                          </label>
                          <input
                            type="date"
                            value={incomeForm.nextPaymentDate || ''}
                            onChange={(e) => setIncomeForm(prev => ({ ...prev, nextPaymentDate: e.target.value || undefined }))}
                            className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          />
                        </div>
                      )}
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">
                          <span className="inline-flex items-center gap-1.5">
                            Tax Profile
                            <InfoTooltip content={TOOLTIP_CONTENT.taxProfile.selector} />
                          </span>
                        </label>
                        <select
                          value={incomeForm.taxProfileId || ''}
                          onChange={(e) => setIncomeForm(prev => ({ ...prev, taxProfileId: e.target.value || undefined }))}
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        >
                          <option value="">No Tax Profile</option>
                          {taxProfiles?.map(tp => (
                            <option key={tp.id} value={tp.id}>{tp.name}</option>
                          ))}
                        </select>
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">
                          <span className="inline-flex items-center gap-1.5">
                            Annual Increase %
                            <InfoTooltip content={TOOLTIP_CONTENT.income.annualIncrease} />
                          </span>
                        </label>
                        <input
                          type="number"
                          step="0.1"
                          value={incomeForm.annualIncreaseRate ? incomeForm.annualIncreaseRate * 100 : ''}
                          onChange={(e) => setIncomeForm(prev => ({ 
                            ...prev, 
                            annualIncreaseRate: e.target.value ? parseFloat(e.target.value) / 100 : undefined 
                          }))}
                          placeholder="e.g., 5 for 5%"
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">
                          <span className="inline-flex items-center gap-1.5">
                            Target Account
                            <InfoTooltip content="The account where this income will be deposited. Used in simulations to track cash flows." />
                          </span>
                        </label>
                        <select
                          value={incomeForm.targetAccountId || ''}
                          onChange={(e) => setIncomeForm(prev => ({ ...prev, targetAccountId: e.target.value || undefined }))}
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        >
                          <option value="">Default (first bank account)</option>
                          {accountsData?.data?.filter(acc => !acc.isLiability).map(acc => (
                            <option key={acc.id} value={acc.id}>{acc.name}</option>
                          ))}
                        </select>
                      </div>
                    </div>

                    <div className="mb-4">
                      <label className="flex items-center gap-2 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={incomeForm.isPreTax}
                          onChange={(e) => setIncomeForm(prev => ({ ...prev, isPreTax: e.target.checked }))}
                          className="w-4 h-4 accent-green-500"
                        />
                        <span className="text-text-primary text-sm inline-flex items-center gap-1.5">
                          Amount is before tax (gross)
                          <InfoTooltip content={TOOLTIP_CONTENT.income.isPreTax} />
                        </span>
                      </label>
                    </div>

                    <div className="mb-4">
                      <label className="block text-sm font-medium text-text-secondary mb-1">Notes</label>
                      <textarea
                        value={incomeForm.notes || ''}
                        onChange={(e) => setIncomeForm(prev => ({ ...prev, notes: e.target.value }))}
                        placeholder="Additional details..."
                        rows={2}
                        className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                      />
                    </div>

                    <div className="flex justify-end gap-3">
                      <button
                        onClick={resetIncomeForm}
                        className="px-4 py-2 bg-background-primary border border-glass-border rounded-lg text-text-secondary hover:text-text-primary transition-colors"
                      >
                        Cancel
                      </button>
                      <button
                        onClick={handleSubmitIncome}
                        disabled={isCreatingIncome || !incomeForm.name || !incomeForm.baseAmount || !incomeForm.targetAccountId}
                        className="px-4 py-2 bg-green-600 rounded-lg text-white font-medium hover:bg-green-700 transition-colors disabled:opacity-50 flex items-center gap-2"
                      >
                        {isCreatingIncome ? (
                          <>
                            <Loader2 className="w-4 h-4 animate-spin" />
                            Saving...
                          </>
                        ) : (
                          <>
                            <Check className="w-4 h-4" />
                            Update
                          </>
                        )}
                      </button>
                    </div>
                  </div>
                ) : (
                  /* Normal Item Row */
                  <div
                    key={income.id}
                    ref={(el) => { if (el) incomeCardRefs.current.set(income.id, el); }}
                    className={cn(
                    "p-4 rounded-lg border transition-colors",
                    income.paymentFrequency.toLowerCase() === 'once'
                      ? "bg-amber-500/5 border-amber-500/30 border-dashed"
                      : income.isActive
                        ? "bg-bg-tertiary border-glass-border"
                        : "bg-bg-tertiary/50 border-glass-border/50 opacity-60"
                  )}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h4 className="text-text-primary font-medium">{income.name}</h4>
                        {income.paymentFrequency.toLowerCase() === 'once' && (
                          <span className="px-2 py-0.5 bg-amber-500/20 text-amber-400 text-xs rounded-full border border-amber-500/30">
                            One-Time
                          </span>
                        )}
                        {!income.isActive && (
                          <span className="px-2 py-0.5 bg-gray-600/20 text-gray-400 text-xs rounded-full">
                            Inactive
                          </span>
                        )}
                      </div>
                      <div className="text-text-secondary text-sm mt-1">
                        {income.employerName && `${income.employerName} • `}
                        {income.paymentFrequency.toLowerCase() === 'once' 
                          ? `Scheduled: ${income.nextPaymentDate ? new Date(income.nextPaymentDate).toLocaleDateString() : 'Not set'}`
                          : income.paymentFrequency} • {income.isPreTax ? 'Gross' : 'Net'}
                        {income.paymentFrequency.toLowerCase() !== 'once' && income.annualIncreaseRate && ` • +${(income.annualIncreaseRate * 100).toFixed(1)}%/yr`}
                        {income.targetAccountName && ` → ${income.targetAccountName}`}
                      </div>
                    </div>
                    <div className="text-right mr-4">
                      <div className="text-green-400 font-semibold">
                        {formatCurrency(income.baseAmount, income.currency)}
                      </div>
                      {income.paymentFrequency.toLowerCase() !== 'once' && (
                        <div className="text-text-secondary text-xs">
                          ≈ {formatCurrency(getMonthlyAmount(income.baseAmount, income.paymentFrequency), income.currency)}/mo
                        </div>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => handleEditIncome(income)}
                        className="p-2 text-text-secondary hover:text-text-primary hover:bg-background-hover rounded transition-colors"
                      >
                        <DollarSign className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => handleDeleteIncome(income.id)}
                        className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                </div>
                )
              ))
            ) : (
              <div className="text-center py-8 text-text-secondary">
                <DollarSign className="w-12 h-12 mx-auto mb-3 opacity-50" />
                <p>No income sources configured yet.</p>
                <p className="text-sm">Add your salary, freelance income, or other revenue streams.</p>
              </div>
            )}
            
            {/* Completed One-Time Income (past scheduled date) */}
            {completedOneTimeIncome.length > 0 && (
              <div className="mt-6 pt-4 border-t border-glass-border/50">
                <h4 className="text-sm font-medium text-text-secondary mb-3">Completed One-Time Income</h4>
                <div className="space-y-2">
                  {completedOneTimeIncome.map((income) => (
                    <div
                      key={income.id}
                      ref={(el) => { if (el) incomeCardRefs.current.set(income.id, el); }}
                      className="p-4 rounded-lg border border-green-500/30 border-dashed bg-green-500/5 transition-colors"
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <h4 className="text-text-primary font-medium">{income.name}</h4>
                            <span className="px-2 py-0.5 bg-green-500/20 text-green-400 text-xs rounded-full border border-green-500/30">
                              ✓ Received
                            </span>
                          </div>
                          <div className="text-text-secondary text-sm mt-1">
                            {income.employerName && `${income.employerName} • `}
                            Received: {income.nextPaymentDate ? new Date(income.nextPaymentDate).toLocaleDateString() : 'Unknown'}
                            {income.targetAccountName && ` → ${income.targetAccountName}`}
                          </div>
                        </div>
                        <div className="text-right mr-4">
                          <div className="text-green-400/70 font-semibold line-through">
                            {formatCurrency(income.baseAmount, income.currency)}
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => handleDeleteIncome(income.id)}
                            className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Expenses Tab */}
      {activeTab === 'expenses' && (
        <div className="space-y-4">
          <div className="flex justify-between items-center">
            <div className="text-sm text-text-secondary">
              Total Monthly: <span className="text-red-400 font-medium">{formatCurrency(totalMonthlyExpenses)}</span>
            </div>
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2">
                <ArrowUpDown className="w-4 h-4 text-text-secondary" />
                <select
                  value={expenseSortBy}
                  onChange={(e) => setExpenseSortBy(e.target.value as SortOption)}
                  className="bg-bg-tertiary border border-glass-border rounded-lg px-2 py-1 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                >
                  <option value="amount-desc">Amount (High to Low)</option>
                  <option value="amount-asc">Amount (Low to High)</option>
                  <option value="name-asc">Name (A-Z)</option>
                  <option value="name-desc">Name (Z-A)</option>
                </select>
              </div>
              <button
                onClick={() => { resetExpenseForm(); setShowExpenseForm(true); }}
                className="px-4 py-2 bg-red-600 rounded-lg text-white font-medium hover:bg-red-700 transition-colors flex items-center gap-2"
              >
                <Plus className="w-4 h-4" />
                Add Expense
              </button>
            </div>
          </div>

          {/* Expense Category Breakdown - Compact Pie Chart */}
          {expenseCategoryPieData.length > 0 && totalMonthlyExpenses > 0 && (
            <div className="p-3 bg-bg-tertiary rounded-lg border border-glass-border">
              <div className="flex flex-col sm:flex-row items-center gap-3">
                <div className="w-24 h-24 flex-shrink-0">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={expenseCategoryPieData}
                        cx="50%"
                        cy="50%"
                        innerRadius="45%"
                        outerRadius="90%"
                        paddingAngle={1}
                        dataKey="value"
                      >
                        {expenseCategoryPieData.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={entry.color} stroke="transparent" />
                        ))}
                      </Pie>
                    </PieChart>
                  </ResponsiveContainer>
                </div>
                <div 
                  className="flex-1 grid gap-x-4 gap-y-1" 
                  style={{ 
                    gridAutoFlow: 'column', 
                    gridTemplateRows: `repeat(${Math.ceil(expenseCategoryPieData.length / 4)}, minmax(0, 1fr))`
                  }}
                >
                  {expenseCategoryPieData.map((item, index) => (
                    <div key={index} className="flex items-center gap-1.5 text-xs">
                      <div 
                        className="w-2 h-2 rounded-full flex-shrink-0"
                        style={{ backgroundColor: item.color }}
                      />
                      <span className="text-text-secondary truncate">{item.name}</span>
                      <span className="text-text-primary font-medium ml-auto">
                        {((item.value / totalMonthlyExpenses) * 100).toFixed(0)}%
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* Expense Form - Only shown when adding NEW (not editing) */}
          {showExpenseForm && !editingExpense && (
            <div ref={newExpenseFormRef} className="p-4 bg-bg-tertiary rounded-lg border border-glass-border">
              <h3 className="text-lg font-medium text-text-primary mb-4">
                New Expense
              </h3>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">Name *</label>
                  <input
                    type="text"
                    value={expenseForm.name}
                    onChange={(e) => setExpenseForm(prev => ({ ...prev, name: e.target.value }))}
                    placeholder="e.g., Rent, Car Payment, Groceries"
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">
                    <span className="inline-flex items-center gap-1.5">
                      Category
                      <InfoTooltip content={TOOLTIP_CONTENT.expenseCategory.general} />
                    </span>
                  </label>
                  <select
                    value={expenseForm.category}
                    onChange={(e) => setExpenseForm(prev => ({ ...prev, category: e.target.value }))}
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  >
                    {expenseCategories.map(cat => (
                      <option key={cat} value={cat}>{cat}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">
                    <span className="inline-flex items-center gap-1.5">
                      Amount *
                      <InfoTooltip content={TOOLTIP_CONTENT.currency.general} />
                    </span>
                  </label>
                  <div className="flex gap-2">
                    <select
                      value={expenseForm.currency}
                      onChange={(e) => setExpenseForm(prev => ({ ...prev, currency: e.target.value }))}
                      className="bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    >
                      <option value="ZAR">ZAR</option>
                      <option value="USD">USD</option>
                      <option value="EUR">EUR</option>
                      <option value="GBP">GBP</option>
                    </select>
                    <input
                      type="number"
                      value={expenseForm.amountValue || ''}
                      onChange={(e) => setExpenseForm(prev => ({ ...prev, amountValue: parseFloat(e.target.value) || 0 }))}
                      placeholder="0.00"
                      className="flex-1 bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">
                    <span className="inline-flex items-center gap-1.5">
                      Frequency
                      <InfoTooltip content={TOOLTIP_CONTENT.frequency.general} />
                    </span>
                  </label>
                  <select
                    value={expenseForm.frequency}
                    onChange={(e) => setExpenseForm(prev => ({ ...prev, frequency: e.target.value as PaymentFrequency }))}
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  >
                    {frequencyOptions.map(opt => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
                </div>
                {/* Scheduled Date for One-Time Expense */}
                {expenseForm.frequency === 'Once' && (
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Scheduled Date *
                        <InfoTooltip content="The date when this one-time expense will occur" />
                      </span>
                    </label>
                    <input
                      type="date"
                      value={expenseForm.startDate || ''}
                      onChange={(e) => setExpenseForm(prev => ({ ...prev, startDate: e.target.value || undefined }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                )}
                {/* Optional Start Date for Recurring Expenses */}
                {expenseForm.frequency !== 'Once' && (
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Start Date (Optional)
                        <InfoTooltip content="When this recurring expense starts. Leave empty to start immediately." />
                      </span>
                    </label>
                    <input
                      type="date"
                      value={expenseForm.startDate || ''}
                      onChange={(e) => setExpenseForm(prev => ({ ...prev, startDate: e.target.value || undefined }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                )}
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">
                    <span className="inline-flex items-center gap-1.5">
                      Source Account
                      <InfoTooltip content="The account from which this expense will be debited. Used in simulations to track cash flows." />
                    </span>
                  </label>
                  <select
                    value={expenseForm.linkedAccountId || ''}
                    onChange={(e) => setExpenseForm(prev => ({ ...prev, linkedAccountId: e.target.value || undefined }))}
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  >
                    <option value="">Default (first bank account)</option>
                    {accountsData?.data?.filter(acc => !acc.isLiability).map(acc => (
                      <option key={acc.id} value={acc.id}>{acc.name}</option>
                    ))}
                  </select>
                </div>
              </div>

              {/* End Condition Section */}
              <div className="mb-4 p-3 bg-background-primary rounded-lg border border-glass-border/50">
                <label className="block text-sm font-medium text-text-secondary mb-2">
                  End Condition
                </label>
                <select
                  value={expenseForm.endConditionType || 'None'}
                  onChange={(e) => setExpenseForm(prev => ({ 
                    ...prev, 
                    endConditionType: e.target.value as CreateExpenseDefinitionRequest['endConditionType'],
                    endConditionAccountId: e.target.value === 'UntilAccountSettled' ? prev.endConditionAccountId : undefined,
                    endDate: e.target.value === 'UntilDate' ? prev.endDate : undefined,
                    endAmountThreshold: e.target.value === 'UntilAmount' ? prev.endAmountThreshold : undefined,
                  }))}
                  className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark] mb-3"
                >
                  {endConditionOptions.map(opt => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
                
                {expenseForm.endConditionType === 'UntilAccountSettled' && (
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      Account to Monitor
                    </label>
                    <select
                      value={expenseForm.endConditionAccountId || ''}
                      onChange={(e) => setExpenseForm(prev => ({ ...prev, endConditionAccountId: e.target.value || undefined }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    >
                      <option value="">Select account...</option>
                      {accountsData?.data?.filter(acc => acc.isLiability).map(acc => (
                        <option key={acc.id} value={acc.id}>{acc.name} (Liability)</option>
                      ))}
                    </select>
                    <p className="text-xs text-text-secondary mt-1">Expense stops when this account balance reaches zero.</p>
                  </div>
                )}
                
                {expenseForm.endConditionType === 'UntilDate' && (
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      End Date
                    </label>
                    <input
                      type="date"
                      value={expenseForm.endDate || ''}
                      onChange={(e) => setExpenseForm(prev => ({ ...prev, endDate: e.target.value || undefined }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                    <p className="text-xs text-text-secondary mt-1">Expense stops after this date.</p>
                  </div>
                )}
                
                {expenseForm.endConditionType === 'UntilAmount' && (
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      Total Amount Threshold
                    </label>
                    <input
                      type="number"
                      value={expenseForm.endAmountThreshold || ''}
                      onChange={(e) => setExpenseForm(prev => ({ ...prev, endAmountThreshold: parseFloat(e.target.value) || undefined }))}
                      placeholder="e.g., 50000"
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                    <p className="text-xs text-text-secondary mt-1">Expense stops after this cumulative amount has been paid.</p>
                  </div>
                )}
              </div>

              <div className="flex gap-4 mb-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={expenseForm.isTaxDeductible}
                    onChange={(e) => setExpenseForm(prev => ({ ...prev, isTaxDeductible: e.target.checked }))}
                    className="w-4 h-4 accent-red-500"
                  />
                  <span className="text-text-primary text-sm inline-flex items-center gap-1.5">
                    Tax Deductible
                    <InfoTooltip content={TOOLTIP_CONTENT.isTaxDeductible} />
                  </span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={expenseForm.inflationAdjusted}
                    onChange={(e) => setExpenseForm(prev => ({ ...prev, inflationAdjusted: e.target.checked }))}
                    className="w-4 h-4 accent-red-500"
                  />
                  <span className="text-text-primary text-sm inline-flex items-center gap-1.5">
                    Inflation Adjusted
                    <InfoTooltip content={TOOLTIP_CONTENT.inflationAdjusted} />
                  </span>
                </label>
              </div>

              <div className="flex justify-end gap-3">
                <button
                  onClick={resetExpenseForm}
                  className="px-4 py-2 bg-background-primary border border-glass-border rounded-lg text-text-secondary hover:text-text-primary transition-colors"
                >
                  Cancel
                </button>
                <button
                  onClick={handleSubmitExpense}
                  disabled={isCreatingExpense || !expenseForm.name || !expenseForm.amountValue}
                  className="px-4 py-2 bg-red-600 rounded-lg text-white font-medium hover:bg-red-700 transition-colors disabled:opacity-50 flex items-center gap-2"
                >
                  {isCreatingExpense ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      Saving...
                    </>
                  ) : (
                    <>
                      <Check className="w-4 h-4" />
                      Create
                    </>
                  )}
                </button>
              </div>
            </div>
          )}

          {/* Expense List */}
          <div className="space-y-2">
            {activeExpenses && activeExpenses.length > 0 ? (
              activeExpenses.map((expense) => (
                editingExpense?.id === expense.id ? (
                  /* Inline Edit Form for Expense */
                  <div key={expense.id} ref={expenseEditFormRef} className="p-4 bg-bg-tertiary rounded-lg border border-accent-purple">
                    <h3 className="text-lg font-medium text-text-primary mb-4">
                      Edit Expense
                    </h3>
                    
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">Name *</label>
                        <input
                          type="text"
                          value={expenseForm.name}
                          onChange={(e) => setExpenseForm(prev => ({ ...prev, name: e.target.value }))}
                          placeholder="e.g., Rent, Car Payment, Groceries"
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">
                          <span className="inline-flex items-center gap-1.5">
                            Category
                            <InfoTooltip content={TOOLTIP_CONTENT.category} />
                          </span>
                        </label>
                        <select
                          value={expenseForm.category}
                          onChange={(e) => setExpenseForm(prev => ({ ...prev, category: e.target.value }))}
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        >
                          {expenseCategories.map(cat => (
                            <option key={cat} value={cat}>{cat}</option>
                          ))}
                        </select>
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">
                          <span className="inline-flex items-center gap-1.5">
                            Amount *
                            <InfoTooltip content={TOOLTIP_CONTENT.currency.general} />
                          </span>
                        </label>
                        <div className="flex gap-2">
                          <select
                            value={expenseForm.currency}
                            onChange={(e) => setExpenseForm(prev => ({ ...prev, currency: e.target.value }))}
                            className="bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          >
                            <option value="ZAR">ZAR</option>
                            <option value="USD">USD</option>
                            <option value="EUR">EUR</option>
                            <option value="GBP">GBP</option>
                          </select>
                          <input
                            type="number"
                            value={expenseForm.amountValue || ''}
                            onChange={(e) => setExpenseForm(prev => ({ ...prev, amountValue: parseFloat(e.target.value) || 0 }))}
                            placeholder="0.00"
                            className="flex-1 bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          />
                        </div>
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">
                          <span className="inline-flex items-center gap-1.5">
                            Frequency
                            <InfoTooltip content={TOOLTIP_CONTENT.frequency.general} />
                          </span>
                        </label>
                        <select
                          value={expenseForm.frequency}
                          onChange={(e) => setExpenseForm(prev => ({ ...prev, frequency: e.target.value as PaymentFrequency }))}
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        >
                          {frequencyOptions.map(opt => (
                            <option key={opt.value} value={opt.value}>{opt.label}</option>
                          ))}
                        </select>
                      </div>
                      {/* Scheduled Date for One-Time Expense (inline edit) */}
                      {expenseForm.frequency === 'Once' && (
                        <div>
                          <label className="block text-sm font-medium text-text-secondary mb-1">
                            <span className="inline-flex items-center gap-1.5">
                              Scheduled Date *
                              <InfoTooltip content="The date when this one-time expense will occur" />
                            </span>
                          </label>
                          <input
                            type="date"
                            value={expenseForm.startDate || ''}
                            onChange={(e) => setExpenseForm(prev => ({ ...prev, startDate: e.target.value || undefined }))}
                            className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          />
                        </div>
                      )}
                      {/* Optional Start Date for Recurring Expenses (inline edit) */}
                      {expenseForm.frequency !== 'Once' && (
                        <div>
                          <label className="block text-sm font-medium text-text-secondary mb-1">
                            <span className="inline-flex items-center gap-1.5">
                              Start Date (Optional)
                              <InfoTooltip content="When this recurring expense starts. Leave empty to start immediately." />
                            </span>
                          </label>
                          <input
                            type="date"
                            value={expenseForm.startDate || ''}
                            onChange={(e) => setExpenseForm(prev => ({ ...prev, startDate: e.target.value || undefined }))}
                            className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          />
                        </div>
                      )}
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">
                          <span className="inline-flex items-center gap-1.5">
                            Source Account
                            <InfoTooltip content="The account from which this expense will be debited. Used in simulations to track cash flows." />
                          </span>
                        </label>
                        <select
                          value={expenseForm.linkedAccountId || ''}
                          onChange={(e) => setExpenseForm(prev => ({ ...prev, linkedAccountId: e.target.value || undefined }))}
                          className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                        >
                          <option value="">Default (first bank account)</option>
                          {accountsData?.data?.filter(acc => !acc.isLiability).map(acc => (
                            <option key={acc.id} value={acc.id}>{acc.name}</option>
                          ))}
                        </select>
                      </div>
                    </div>

                    {/* End Condition Section (Edit Form) */}
                    <div className="mb-4 p-3 bg-background-primary rounded-lg border border-glass-border/50">
                      <label className="block text-sm font-medium text-text-secondary mb-2">
                        End Condition
                      </label>
                      <select
                        value={expenseForm.endConditionType || 'None'}
                        onChange={(e) => setExpenseForm(prev => ({ 
                          ...prev, 
                          endConditionType: e.target.value as CreateExpenseDefinitionRequest['endConditionType'],
                          endConditionAccountId: e.target.value === 'UntilAccountSettled' ? prev.endConditionAccountId : undefined,
                          endDate: e.target.value === 'UntilDate' ? prev.endDate : undefined,
                          endAmountThreshold: e.target.value === 'UntilAmount' ? prev.endAmountThreshold : undefined,
                        }))}
                        className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark] mb-3"
                      >
                        {endConditionOptions.map(opt => (
                          <option key={opt.value} value={opt.value}>{opt.label}</option>
                        ))}
                      </select>
                      
                      {expenseForm.endConditionType === 'UntilAccountSettled' && (
                        <div>
                          <label className="block text-sm font-medium text-text-secondary mb-1">
                            Account to Monitor
                          </label>
                          <select
                            value={expenseForm.endConditionAccountId || ''}
                            onChange={(e) => setExpenseForm(prev => ({ ...prev, endConditionAccountId: e.target.value || undefined }))}
                            className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          >
                            <option value="">Select account...</option>
                            {accountsData?.data?.filter(acc => acc.isLiability).map(acc => (
                              <option key={acc.id} value={acc.id}>{acc.name} (Liability)</option>
                            ))}
                          </select>
                          <p className="text-xs text-text-secondary mt-1">Expense stops when this account balance reaches zero.</p>
                        </div>
                      )}
                      
                      {expenseForm.endConditionType === 'UntilDate' && (
                        <div>
                          <label className="block text-sm font-medium text-text-secondary mb-1">
                            End Date
                          </label>
                          <input
                            type="date"
                            value={expenseForm.endDate || ''}
                            onChange={(e) => setExpenseForm(prev => ({ ...prev, endDate: e.target.value || undefined }))}
                            className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          />
                          <p className="text-xs text-text-secondary mt-1">Expense stops after this date.</p>
                        </div>
                      )}
                      
                      {expenseForm.endConditionType === 'UntilAmount' && (
                        <div>
                          <label className="block text-sm font-medium text-text-secondary mb-1">
                            Total Amount Threshold
                          </label>
                          <input
                            type="number"
                            value={expenseForm.endAmountThreshold || ''}
                            onChange={(e) => setExpenseForm(prev => ({ ...prev, endAmountThreshold: parseFloat(e.target.value) || undefined }))}
                            placeholder="e.g., 50000"
                            className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                          />
                          <p className="text-xs text-text-secondary mt-1">Expense stops after this cumulative amount has been paid.</p>
                        </div>
                      )}
                    </div>

                    <div className="flex gap-4 mb-4">
                      <label className="flex items-center gap-2 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={expenseForm.isTaxDeductible}
                          onChange={(e) => setExpenseForm(prev => ({ ...prev, isTaxDeductible: e.target.checked }))}
                          className="w-4 h-4 accent-red-500"
                        />
                        <span className="text-text-primary text-sm inline-flex items-center gap-1.5">
                          Tax Deductible
                          <InfoTooltip content={TOOLTIP_CONTENT.isTaxDeductible} />
                        </span>
                      </label>
                      <label className="flex items-center gap-2 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={expenseForm.inflationAdjusted}
                          onChange={(e) => setExpenseForm(prev => ({ ...prev, inflationAdjusted: e.target.checked }))}
                          className="w-4 h-4 accent-red-500"
                        />
                        <span className="text-text-primary text-sm inline-flex items-center gap-1.5">
                          Inflation Adjusted
                          <InfoTooltip content={TOOLTIP_CONTENT.inflationAdjusted} />
                        </span>
                      </label>
                    </div>

                    <div className="flex justify-end gap-3">
                      <button
                        onClick={resetExpenseForm}
                        className="px-4 py-2 bg-background-primary border border-glass-border rounded-lg text-text-secondary hover:text-text-primary transition-colors"
                      >
                        Cancel
                      </button>
                      <button
                        onClick={handleSubmitExpense}
                        disabled={isCreatingExpense || !expenseForm.name || !expenseForm.amountValue}
                        className="px-4 py-2 bg-red-600 rounded-lg text-white font-medium hover:bg-red-700 transition-colors disabled:opacity-50 flex items-center gap-2"
                      >
                        {isCreatingExpense ? (
                          <>
                            <Loader2 className="w-4 h-4 animate-spin" />
                            Saving...
                          </>
                        ) : (
                          <>
                            <Check className="w-4 h-4" />
                            Update
                          </>
                        )}
                      </button>
                    </div>
                  </div>
                ) : (
                  /* Normal Expense Row */
                  <div
                    key={expense.id}
                    ref={(el) => { if (el) expenseCardRefs.current.set(expense.id, el); }}
                    className={cn(
                      "p-4 rounded-lg border transition-colors",
                      expense.isActive
                        ? expense.frequency.toLowerCase() === 'once'
                          ? "bg-amber-500/5 border-amber-500/30 border-dashed"
                          : "bg-bg-tertiary border-glass-border"
                        : "bg-bg-tertiary/50 border-glass-border/50 opacity-60"
                    )}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <h4 className="text-text-primary font-medium">{expense.name}</h4>
                          <span className="px-2 py-0.5 bg-accent-purple/20 text-accent-purple text-xs rounded-full">
                            {expense.category}
                          </span>
                          {expense.frequency.toLowerCase() === 'once' && (
                            <span className="px-2 py-0.5 bg-amber-500/20 text-amber-400 text-xs rounded-full border border-amber-500/30">
                              One-Time
                            </span>
                          )}
                          {!expense.isActive && (
                            <span className="px-2 py-0.5 bg-gray-600/20 text-gray-400 text-xs rounded-full">
                              Inactive
                            </span>
                          )}
                        </div>
                        <div className="text-text-secondary text-sm mt-1">
                          {expense.frequency.toLowerCase() === 'once'
                            ? `Scheduled: ${expense.startDate ? new Date(expense.startDate).toLocaleDateString() : 'Not set'}`
                            : expense.startDate 
                              ? `${expense.frequency} • Starts: ${new Date(expense.startDate).toLocaleDateString()}`
                              : expense.frequency}
                          {expense.linkedAccountName && ` • From: ${expense.linkedAccountName}`}
                          {expense.isTaxDeductible && ' • Tax Deductible'}
                          {expense.inflationAdjusted && ' • Inflation Adj.'}
                        </div>
                      </div>
                      <div className="text-right mr-4">
                        <div className="text-red-400 font-semibold">
                          {formatCurrency(expense.amountValue || 0, expense.currency)}
                        </div>
                        {expense.frequency.toLowerCase() !== 'once' && (
                          <div className="text-text-secondary text-xs">
                            ≈ {formatCurrency(getMonthlyAmount(expense.amountValue || 0, expense.frequency), expense.currency)}/mo
                          </div>
                        )}
                      </div>
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => handleEditExpense(expense)}
                          className="p-2 text-text-secondary hover:text-text-primary hover:bg-background-hover rounded transition-colors"
                        >
                          <DollarSign className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleDeleteExpense(expense.id)}
                          className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  </div>
                )
              ))
            ) : (
              <div className="text-center py-8 text-text-secondary">
                <DollarSign className="w-12 h-12 mx-auto mb-3 opacity-50" />
                <p>No expenses configured yet.</p>
                <p className="text-sm">Add your recurring bills, subscriptions, and regular expenses.</p>
              </div>
            )}
            
            {/* Completed One-Time Expenses (past scheduled date) */}
            {completedOneTimeExpenses.length > 0 && (
              <div className="mt-6 pt-4 border-t border-glass-border/50">
                <h4 className="text-sm font-medium text-text-secondary mb-3">Completed One-Time Expenses</h4>
                <div className="space-y-2">
                  {completedOneTimeExpenses.map((expense) => (
                    editingExpense?.id === expense.id ? (
                      /* Inline Edit Form for Completed Expense */
                      <div key={expense.id} ref={expenseEditFormRef} className="p-4 bg-bg-tertiary rounded-lg border border-accent-purple">
                        <h3 className="text-lg font-medium text-text-primary mb-4">
                          Edit Expense
                        </h3>
                        {/* Same edit form content - render handled by React */}
                      </div>
                    ) : (
                      <div
                        key={expense.id}
                        ref={(el) => { if (el) expenseCardRefs.current.set(expense.id, el); }}
                        className="p-4 rounded-lg border border-green-500/30 border-dashed bg-green-500/5 transition-colors"
                      >
                        <div className="flex items-center justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-2">
                              <h4 className="text-text-primary font-medium">{expense.name}</h4>
                              <span className="px-2 py-0.5 bg-accent-purple/20 text-accent-purple text-xs rounded-full">
                                {expense.category}
                              </span>
                              <span className="px-2 py-0.5 bg-green-500/20 text-green-400 text-xs rounded-full border border-green-500/30">
                                ✓ Completed
                              </span>
                            </div>
                            <div className="text-text-secondary text-sm mt-1">
                              Paid: {expense.startDate ? new Date(expense.startDate).toLocaleDateString() : 'Unknown'}
                              {expense.linkedAccountName && ` • From: ${expense.linkedAccountName}`}
                            </div>
                          </div>
                          <div className="text-right mr-4">
                            <div className="text-green-400/70 font-semibold line-through">
                              {formatCurrency(expense.amountValue || 0, expense.currency)}
                            </div>
                          </div>
                          <div className="flex items-center gap-2">
                            <button
                              onClick={() => handleDeleteExpense(expense.id)}
                              className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </div>
                        </div>
                      </div>
                    )
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </GlassCard>
  );
}

export function InvestmentSettings() {
  const { data: contributionsData, isLoading } = useGetInvestmentContributionsQuery();
  const { data: accounts } = useGetAccountsQuery();
  
  const [createContribution, { isLoading: isCreating }] = useCreateInvestmentContributionMutation();
  const [updateContribution] = useUpdateInvestmentContributionMutation();
  const [deleteContribution] = useDeleteInvestmentContributionMutation();
  
  const [showForm, setShowForm] = useState(false);
  const [editingContribution, setEditingContribution] = useState<InvestmentContribution | null>(null);
  
  // Refs for auto-scrolling investment contribution forms
  const investmentEditFormRef = useRef<HTMLDivElement>(null);
  const newInvestmentFormRef = useRef<HTMLDivElement>(null);
  const investmentCardRefs = useRef<Map<string, HTMLDivElement>>(new Map());
  const [lastSavedInvestmentId, setLastSavedInvestmentId] = useState<string | null>(null);
  
  // Scroll to saved item card
  useEffect(() => {
    if (lastSavedInvestmentId) {
      setTimeout(() => {
        const cardElement = investmentCardRefs.current.get(lastSavedInvestmentId);
        if (cardElement) {
          cardElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        setLastSavedInvestmentId(null);
      }, 150);
    }
  }, [lastSavedInvestmentId]);
  
  // Auto-scroll to investment contribution edit form
  useEffect(() => {
    if (editingContribution && investmentEditFormRef.current) {
      setTimeout(() => {
        const element = investmentEditFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20;
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [editingContribution]);

  useEffect(() => {
    if (showForm && !editingContribution && newInvestmentFormRef.current) {
      setTimeout(() => {
        const element = newInvestmentFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20;
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [showForm, editingContribution]);

  const [formData, setFormData] = useState<CreateInvestmentContributionRequest>({
    name: '',
    currency: 'ZAR',
    amount: 0,
    frequency: 'Monthly',
    category: '',
    sourceAccountId: undefined,
    startDate: undefined,
    endConditionType: 'None',
    endConditionAccountId: undefined,
    endDate: undefined,
    endAmountThreshold: undefined,
  });

  const contributions = contributionsData?.sources || [];
  const summary = contributionsData?.summary;

  const frequencyOptions = [
    { value: 'Monthly', label: 'Monthly' },
    { value: 'Annually', label: 'Annually' },
    { value: 'Weekly', label: 'Weekly' },
    { value: 'Biweekly', label: 'Bi-Weekly' },
    { value: 'Once', label: 'One-Time' },
  ];

  const categoryOptions = [
    'Retirement',
    'Emergency Fund',
    'Short-Term Savings',
    'Long-Term Savings',
    'Debt Repayment',
    'Investment',
    'Goal-based',
    'Other',
  ];

  const endConditionOptions = [
    { value: 'None', label: 'No end condition (runs indefinitely)' },
    { value: 'UntilAccountSettled', label: 'Until account is settled (balance = 0)' },
    { value: 'UntilDate', label: 'Until a specific date' },
    { value: 'UntilAmount', label: 'Until total amount contributed' },
  ];

  const resetForm = () => {
    setFormData({
      name: '',
      currency: 'ZAR',
      amount: 0,
      frequency: 'Monthly',
      category: '',
      sourceAccountId: undefined,
      startDate: undefined,
      endConditionType: 'None',
      endConditionAccountId: undefined,
      endDate: undefined,
      endAmountThreshold: undefined,
    });
    setEditingContribution(null);
    setShowForm(false);
  };

  const handleEdit = (contribution: InvestmentContribution) => {
    setEditingContribution(contribution);
    // Map the end condition type from API format (camelCase) to our format (PascalCase)
    const endConditionTypeMap: Record<string, EndConditionType> = {
      'none': 'None',
      'None': 'None',
      'untilaccountsettled': 'UntilAccountSettled',
      'untilAccountSettled': 'UntilAccountSettled',
      'UntilAccountSettled': 'UntilAccountSettled',
      'untildate': 'UntilDate',
      'untilDate': 'UntilDate',
      'UntilDate': 'UntilDate',
      'untilamount': 'UntilAmount',
      'untilAmount': 'UntilAmount',
      'UntilAmount': 'UntilAmount',
    };
    // Map the frequency from API format (camelCase) to our format (PascalCase)
    const frequencyMap: Record<string, PaymentFrequency> = {
      'monthly': 'Monthly',
      'Monthly': 'Monthly',
      'annual': 'Annually',
      'Annual': 'Annually',
      'annually': 'Annually',
      'Annually': 'Annually',
      'weekly': 'Weekly',
      'Weekly': 'Weekly',
      'biweekly': 'Biweekly',
      'Biweekly': 'Biweekly',
      'once': 'Once',
      'Once': 'Once',
    };
    setFormData({
      name: contribution.name,
      currency: contribution.currency,
      amount: contribution.amount,
      frequency: frequencyMap[contribution.frequency] || 'Monthly',
      targetAccountId: contribution.targetAccountId,
      sourceAccountId: contribution.sourceAccountId,
      category: contribution.category || '',
      annualIncreaseRate: contribution.annualIncreaseRate,
      notes: contribution.notes,
      startDate: contribution.startDate,
      endConditionType: endConditionTypeMap[contribution.endConditionType] || 'None',
      endConditionAccountId: contribution.endConditionAccountId,
      endDate: contribution.endDate,
      endAmountThreshold: contribution.endAmountThreshold,
    });
    setShowForm(true);
  };

  const handleSubmit = async () => {
    try {
      const savedId = editingContribution?.id;
      if (editingContribution) {
        await updateContribution({
          id: editingContribution.id,
          ...formData,
        }).unwrap();
      } else {
        await createContribution(formData as CreateInvestmentContributionRequest).unwrap();
      }
      resetForm();
      // Scroll to the saved item card after edit
      if (savedId) {
        setLastSavedInvestmentId(savedId);
      }
    } catch (err) {
      console.error('Failed to save investment contribution:', err);
    }
  };

  const handleDelete = async (id: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to delete this investment contribution?',
    });
    if (confirmed) {
      await deleteContribution(id);
    }
  };

  const formatCurrency = (amount: number, currency: string = 'ZAR') => {
    const symbols: Record<string, string> = { ZAR: 'R', USD: '$', EUR: '€', GBP: '£' };
    return `${symbols[currency] || currency} ${amount.toLocaleString()}`;
  };

  const getMonthlyAmount = (amount: number, frequency: string) => {
    const freq = frequency.toLowerCase();
    switch (freq) {
      case 'annual':
      case 'annually': return amount / 12;
      case 'weekly': return amount * 4.33;
      case 'biweekly': return amount * 2.17;
      case 'once': return 0; // One-time items don't contribute to monthly totals
      case 'quarterly': return amount / 3;
      default: return amount; // monthly
    }
  };

  // Check if a scheduled date is in the past (for one-time contributions)
  const isScheduledDatePast = (dateStr?: string): boolean => {
    if (!dateStr) return false;
    const date = new Date(dateStr);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return date < today;
  };

  // Separate active vs completed one-time investments
  const activeContributions = contributions.filter(c => {
    if (c.frequency.toLowerCase() !== 'once') return true;
    return !isScheduledDatePast(c.startDate);
  });

  const completedOneTimeContributions = contributions.filter(c => {
    return c.frequency.toLowerCase() === 'once' && isScheduledDatePast(c.startDate);
  });

  if (isLoading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex justify-center py-8">
          <Loader2 className="w-6 h-6 animate-spin text-accent-purple" />
        </div>
      </GlassCard>
    );
  }

  return (
    <GlassCard variant="default" className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-xl font-semibold text-text-primary">Investment Contributions</h2>
          <p className="text-text-secondary text-sm mt-1">
            Configure recurring contributions to savings, investments, and debt repayment.
          </p>
        </div>
        <div className="text-right">
          <div className="text-sm text-text-secondary">Total Monthly Contributions</div>
          <div className="text-xl font-semibold text-accent-purple">
            {formatCurrency(summary?.totalMonthlyContributions || 0)}
          </div>
        </div>
      </div>

      {/* Category Summary */}
      {summary?.byCategory && Object.keys(summary.byCategory).length > 0 && (
        <div className="mb-6 p-4 bg-bg-tertiary rounded-lg border border-glass-border">
          <h3 className="text-sm font-medium text-text-primary mb-3">Contributions by Category</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            {Object.entries(summary.byCategory).map(([category, amount]) => (
              <div key={category}>
                <div className="text-text-secondary">{category}</div>
                <div className="text-accent-purple font-medium">{formatCurrency(amount)}/mo</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Add Button */}
      <div className="flex justify-end mb-4">
        <button
          onClick={() => { resetForm(); setShowForm(true); }}
          className="px-4 py-2 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors flex items-center gap-2"
        >
          <Plus className="w-4 h-4" />
          Add Contribution
        </button>
      </div>

      {/* Form - Only shown when adding NEW (not editing) */}
      {showForm && !editingContribution && (
        <div ref={newInvestmentFormRef} className="mb-6 p-4 bg-bg-tertiary rounded-lg border border-glass-border">
          <h3 className="text-lg font-medium text-text-primary mb-4">
            New Investment Contribution
          </h3>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">Name *</label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                placeholder="e.g., Retirement Fund, Emergency Savings"
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Category
                  <InfoTooltip content={TOOLTIP_CONTENT.investmentCategory.general} />
                </span>
              </label>
              <select
                value={formData.category || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, category: e.target.value }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              >
                <option value="">Select category...</option>
                {categoryOptions.map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Amount *
                  <InfoTooltip content={TOOLTIP_CONTENT.currency.general} />
                </span>
              </label>
              <div className="flex gap-2">
                <select
                  value={formData.currency}
                  onChange={(e) => setFormData(prev => ({ ...prev, currency: e.target.value }))}
                  className="bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                >
                  <option value="ZAR">ZAR</option>
                  <option value="USD">USD</option>
                  <option value="EUR">EUR</option>
                  <option value="GBP">GBP</option>
                </select>
                <input
                  type="number"
                  value={formData.amount || ''}
                  onChange={(e) => setFormData(prev => ({ ...prev, amount: parseFloat(e.target.value) || 0 }))}
                  placeholder="0.00"
                  className="flex-1 bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Frequency
                  <InfoTooltip content={TOOLTIP_CONTENT.frequency.general} />
                </span>
              </label>
              <select
                value={formData.frequency}
                onChange={(e) => setFormData(prev => ({ ...prev, frequency: e.target.value as PaymentFrequency }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              >
                {frequencyOptions.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
            {/* Start Date - Required for one-time contributions */}
            {formData.frequency === 'Once' && (
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-1">
                  <span className="inline-flex items-center gap-1.5">
                    Date *
                    <InfoTooltip content="The date when this one-time contribution should be applied in simulations." />
                  </span>
                </label>
                <input
                  type="date"
                  value={formData.startDate || ''}
                  onChange={(e) => setFormData(prev => ({ ...prev, startDate: e.target.value || undefined }))}
                  className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                />
              </div>
            )}
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Target Account
                  <InfoTooltip content={TOOLTIP_CONTENT.targetAccount} />
                </span>
              </label>
              <select
                value={formData.targetAccountId || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, targetAccountId: e.target.value || undefined }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              >
                <option value="">No linked account</option>
                {accounts?.data?.map(acc => (
                  <option key={acc.id} value={acc.id}>{acc.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Source Account
                  <InfoTooltip content={TOOLTIP_CONTENT.sourceAccount} />
                </span>
              </label>
              <select
                value={formData.sourceAccountId || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, sourceAccountId: e.target.value || undefined }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              >
                <option value="">No source account</option>
                {accounts?.data?.map(acc => (
                  <option key={acc.id} value={acc.id}>{acc.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">Annual Increase %</label>
              <input
                type="number"
                step="0.1"
                value={formData.annualIncreaseRate ? formData.annualIncreaseRate * 100 : ''}
                onChange={(e) => setFormData(prev => ({ 
                  ...prev, 
                  annualIncreaseRate: e.target.value ? parseFloat(e.target.value) / 100 : undefined 
                }))}
                placeholder="e.g., 5 for 5%"
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium text-text-secondary mb-1">Notes</label>
            <textarea
              value={formData.notes || ''}
              onChange={(e) => setFormData(prev => ({ ...prev, notes: e.target.value }))}
              placeholder="Additional details..."
              rows={2}
              className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
            />
          </div>

          {/* End Condition Section */}
          <div className="mb-4 p-3 bg-background-primary rounded-lg border border-glass-border/50">
            <label className="block text-sm font-medium text-text-secondary mb-2">
              End Condition
            </label>
            <select
              value={formData.endConditionType || 'None'}
              onChange={(e) => setFormData(prev => ({ 
                ...prev, 
                endConditionType: e.target.value as EndConditionType,
                endConditionAccountId: e.target.value === 'UntilAccountSettled' ? prev.endConditionAccountId : undefined,
                endDate: e.target.value === 'UntilDate' ? prev.endDate : undefined,
                endAmountThreshold: e.target.value === 'UntilAmount' ? prev.endAmountThreshold : undefined,
              }))}
              className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark] mb-3"
            >
              {endConditionOptions.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
            
            {formData.endConditionType === 'UntilAccountSettled' && (
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-1">
                  Account to Monitor
                </label>
                <select
                  value={formData.endConditionAccountId || ''}
                  onChange={(e) => setFormData(prev => ({ ...prev, endConditionAccountId: e.target.value || undefined }))}
                  className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                >
                  <option value="">Select account...</option>
                  {accounts?.data?.filter(acc => acc.isLiability).map(acc => (
                    <option key={acc.id} value={acc.id}>{acc.name} (Liability)</option>
                  ))}
                </select>
                <p className="text-xs text-text-secondary mt-1">Contribution stops when this account balance reaches zero.</p>
              </div>
            )}
            
            {formData.endConditionType === 'UntilDate' && (
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-1">
                  End Date
                </label>
                <input
                  type="date"
                  value={formData.endDate || ''}
                  onChange={(e) => setFormData(prev => ({ ...prev, endDate: e.target.value || undefined }))}
                  className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                />
                <p className="text-xs text-text-secondary mt-1">Contribution stops after this date.</p>
              </div>
            )}
            
            {formData.endConditionType === 'UntilAmount' && (
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-1">
                  Total Amount Threshold
                </label>
                <input
                  type="number"
                  value={formData.endAmountThreshold || ''}
                  onChange={(e) => setFormData(prev => ({ ...prev, endAmountThreshold: parseFloat(e.target.value) || undefined }))}
                  placeholder="e.g., 50000"
                  className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                />
                <p className="text-xs text-text-secondary mt-1">Contribution stops after this cumulative amount has been contributed.</p>
              </div>
            )}
          </div>

          <div className="flex justify-end gap-3">
            <button
              onClick={resetForm}
              className="px-4 py-2 bg-background-primary border border-glass-border rounded-lg text-text-secondary hover:text-text-primary transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleSubmit}
              disabled={isCreating || !formData.name || !formData.amount || !formData.targetAccountId || !formData.sourceAccountId || (formData.frequency === 'Once' && !formData.startDate)}
              className="px-4 py-2 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors disabled:opacity-50 flex items-center gap-2"
            >
              {isCreating ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  Create
                </>
              )}
            </button>
          </div>
        </div>
      )}

      {/* Contributions List */}
      <div className="space-y-2">
        {activeContributions.length > 0 ? (
          activeContributions.map((contribution) => (
            editingContribution?.id === contribution.id ? (
              /* Inline Edit Form for Investment Contribution */
              <div key={contribution.id} ref={investmentEditFormRef} className="p-4 bg-bg-tertiary rounded-lg border border-accent-purple">
                <h3 className="text-lg font-medium text-text-primary mb-4">
                  Edit Investment Contribution
                </h3>
                
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">Name *</label>
                    <input
                      type="text"
                      value={formData.name}
                      onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                      placeholder="e.g., Retirement Fund, Emergency Savings"
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Category
                        <InfoTooltip content={TOOLTIP_CONTENT.investmentCategory.general} />
                      </span>
                    </label>
                    <select
                      value={formData.category || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, category: e.target.value }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    >
                      <option value="">Select category</option>
                      {categoryOptions.map(cat => (
                        <option key={cat} value={cat}>{cat}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Amount *
                        <InfoTooltip content={TOOLTIP_CONTENT.currency.general} />
                      </span>
                    </label>
                    <div className="flex gap-2">
                      <select
                        value={formData.currency}
                        onChange={(e) => setFormData(prev => ({ ...prev, currency: e.target.value }))}
                        className="bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                      >
                        <option value="ZAR">ZAR</option>
                        <option value="USD">USD</option>
                        <option value="EUR">EUR</option>
                        <option value="GBP">GBP</option>
                      </select>
                      <input
                        type="number"
                        value={formData.amount || ''}
                        onChange={(e) => setFormData(prev => ({ ...prev, amount: parseFloat(e.target.value) || 0 }))}
                        placeholder="0.00"
                        className="flex-1 bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                      />
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Frequency
                        <InfoTooltip content={TOOLTIP_CONTENT.frequency.general} />
                      </span>
                    </label>
                    <select
                      value={formData.frequency}
                      onChange={(e) => setFormData(prev => ({ ...prev, frequency: e.target.value as PaymentFrequency }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    >
                      {frequencyOptions.map(opt => (
                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                      ))}
                    </select>
                  </div>
                  {/* Start Date - Required for one-time contributions (Edit Form) */}
                  {formData.frequency === 'Once' && (
                    <div>
                      <label className="block text-sm font-medium text-text-secondary mb-1">
                        <span className="inline-flex items-center gap-1.5">
                          Date *
                          <InfoTooltip content="The date when this one-time contribution should be applied in simulations." />
                        </span>
                      </label>
                      <input
                        type="date"
                        value={formData.startDate || ''}
                        onChange={(e) => setFormData(prev => ({ ...prev, startDate: e.target.value || undefined }))}
                        className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                      />
                    </div>
                  )}
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Target Account
                        <InfoTooltip content={TOOLTIP_CONTENT.targetAccount} />
                      </span>
                    </label>
                    <select
                      value={formData.targetAccountId || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, targetAccountId: e.target.value || undefined }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    >
                      <option value="">No linked account</option>
                      {accounts?.data?.map(acc => (
                        <option key={acc.id} value={acc.id}>{acc.name}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Source Account
                        <InfoTooltip content={TOOLTIP_CONTENT.sourceAccount} />
                      </span>
                    </label>
                    <select
                      value={formData.sourceAccountId || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, sourceAccountId: e.target.value || undefined }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    >
                      <option value="">No source account</option>
                      {accounts?.data?.map(acc => (
                        <option key={acc.id} value={acc.id}>{acc.name}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">Annual Increase %</label>
                    <input
                      type="number"
                      step="0.1"
                      value={formData.annualIncreaseRate ? formData.annualIncreaseRate * 100 : ''}
                      onChange={(e) => setFormData(prev => ({ 
                        ...prev, 
                        annualIncreaseRate: e.target.value ? parseFloat(e.target.value) / 100 : undefined 
                      }))}
                      placeholder="e.g., 5 for 5%"
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                </div>

                <div className="mb-4">
                  <label className="block text-sm font-medium text-text-secondary mb-1">Notes</label>
                  <textarea
                    value={formData.notes || ''}
                    onChange={(e) => setFormData(prev => ({ ...prev, notes: e.target.value }))}
                    placeholder="Additional details..."
                    rows={2}
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  />
                </div>

                {/* End Condition Section */}
                <div className="mb-4 p-3 bg-background-primary rounded-lg border border-glass-border/50">
                  <label className="block text-sm font-medium text-text-secondary mb-2">
                    End Condition
                  </label>
                  <select
                    value={formData.endConditionType || 'None'}
                    onChange={(e) => setFormData(prev => ({ 
                      ...prev, 
                      endConditionType: e.target.value as EndConditionType,
                      endConditionAccountId: e.target.value === 'UntilAccountSettled' ? prev.endConditionAccountId : undefined,
                      endDate: e.target.value === 'UntilDate' ? prev.endDate : undefined,
                      endAmountThreshold: e.target.value === 'UntilAmount' ? prev.endAmountThreshold : undefined,
                    }))}
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark] mb-3"
                  >
                    {endConditionOptions.map(opt => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
                  
                  {formData.endConditionType === 'UntilAccountSettled' && (
                    <div>
                      <label className="block text-sm font-medium text-text-secondary mb-1">
                        Account to Monitor
                      </label>
                      <select
                        value={formData.endConditionAccountId || ''}
                        onChange={(e) => setFormData(prev => ({ ...prev, endConditionAccountId: e.target.value || undefined }))}
                        className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                      >
                        <option value="">Select account...</option>
                        {accounts?.data?.filter(acc => acc.isLiability).map(acc => (
                          <option key={acc.id} value={acc.id}>{acc.name} (Liability)</option>
                        ))}
                      </select>
                      <p className="text-xs text-text-secondary mt-1">Contribution stops when this account balance reaches zero.</p>
                    </div>
                  )}
                  
                  {formData.endConditionType === 'UntilDate' && (
                    <div>
                      <label className="block text-sm font-medium text-text-secondary mb-1">
                        End Date
                      </label>
                      <input
                        type="date"
                        value={formData.endDate || ''}
                        onChange={(e) => setFormData(prev => ({ ...prev, endDate: e.target.value || undefined }))}
                        className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                      />
                      <p className="text-xs text-text-secondary mt-1">Contribution stops after this date.</p>
                    </div>
                  )}
                  
                  {formData.endConditionType === 'UntilAmount' && (
                    <div>
                      <label className="block text-sm font-medium text-text-secondary mb-1">
                        Total Amount Threshold
                      </label>
                      <input
                        type="number"
                        value={formData.endAmountThreshold || ''}
                        onChange={(e) => setFormData(prev => ({ ...prev, endAmountThreshold: parseFloat(e.target.value) || undefined }))}
                        placeholder="e.g., 50000"
                        className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                      />
                      <p className="text-xs text-text-secondary mt-1">Contribution stops after this cumulative amount has been contributed.</p>
                    </div>
                  )}
                </div>

                <div className="flex justify-end gap-3">
                  <button
                    onClick={resetForm}
                    className="px-4 py-2 bg-background-primary border border-glass-border rounded-lg text-text-secondary hover:text-text-primary transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleSubmit}
                    disabled={isCreating || !formData.name || !formData.amount || !formData.targetAccountId || !formData.sourceAccountId || (formData.frequency === 'Once' && !formData.startDate)}
                    className="px-4 py-2 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors disabled:opacity-50 flex items-center gap-2"
                  >
                    {isCreating ? (
                      <>
                        <Loader2 className="w-4 h-4 animate-spin" />
                        Saving...
                      </>
                    ) : (
                      <>
                        <Check className="w-4 h-4" />
                        Update
                      </>
                    )}
                  </button>
                </div>
              </div>
            ) : (
              /* Normal Contribution Row */
              <div
                key={contribution.id}
                ref={(el) => { if (el) investmentCardRefs.current.set(contribution.id, el); }}
                className={cn(
                  "p-4 rounded-lg border transition-colors",
                  contribution.isActive
                    ? "bg-bg-tertiary border-glass-border"
                    : "bg-bg-tertiary/50 border-glass-border/50 opacity-60"
                )}
              >
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <h4 className="text-text-primary font-medium">{contribution.name}</h4>
                      {contribution.category && (
                        <span className="px-2 py-0.5 bg-accent-purple/20 text-accent-purple text-xs rounded-full">
                          {contribution.category}
                        </span>
                      )}
                      {!contribution.isActive && (
                        <span className="px-2 py-0.5 bg-gray-600/20 text-gray-400 text-xs rounded-full">
                          Inactive
                        </span>
                      )}
                      {contribution.endConditionType && contribution.endConditionType.toLowerCase() !== 'none' && (
                        <span className="px-2 py-0.5 bg-yellow-500/20 text-yellow-400 text-xs rounded-full">
                          {contribution.endConditionType.toLowerCase() === 'untilaccountsettled' && `Until ${contribution.endConditionAccountName || 'account'} settled`}
                          {contribution.endConditionType.toLowerCase() === 'untildate' && `Until ${contribution.endDate}`}
                          {contribution.endConditionType.toLowerCase() === 'untilamount' && `Until R${contribution.endAmountThreshold?.toLocaleString()}`}
                        </span>
                      )}
                    </div>
                    <div className="text-text-secondary text-sm mt-1">
                      {contribution.frequency.toLowerCase() === 'once' 
                        ? `One-Time${contribution.startDate ? ` on ${contribution.startDate}` : ''}` 
                        : contribution.frequency}
                      {contribution.sourceAccountName && ` • From: ${contribution.sourceAccountName}`}
                      {contribution.targetAccountName && ` → ${contribution.targetAccountName}`}
                      {contribution.annualIncreaseRate && ` • +${(contribution.annualIncreaseRate * 100).toFixed(1)}%/yr`}
                    </div>
                  </div>
                  <div className="text-right mr-4">
                    <div className="text-accent-purple font-semibold">
                      {formatCurrency(contribution.amount, contribution.currency)}
                    </div>
                    {contribution.frequency.toLowerCase() !== 'once' && (
                      <div className="text-text-secondary text-xs">
                        ≈ {formatCurrency(getMonthlyAmount(contribution.amount, contribution.frequency), contribution.currency)}/mo
                      </div>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => handleEdit(contribution)}
                      className="p-2 text-text-secondary hover:text-text-primary hover:bg-background-hover rounded transition-colors"
                    >
                      <TrendingUp className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => handleDelete(contribution.id)}
                      className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </div>
            )
          ))
        ) : (
          <div className="text-center py-8 text-text-secondary">
            <TrendingUp className="w-12 h-12 mx-auto mb-3 opacity-50" />
            <p>No investment contributions configured yet.</p>
            <p className="text-sm">Add your recurring savings, investments, and debt repayment allocations.</p>
          </div>
        )}
        
        {/* Completed One-Time Investments (past scheduled date) */}
        {completedOneTimeContributions.length > 0 && (
          <div className="mt-6 pt-4 border-t border-glass-border/50">
            <h4 className="text-sm font-medium text-text-secondary mb-3">Completed One-Time Investments</h4>
            <div className="space-y-2">
              {completedOneTimeContributions.map((contribution) => (
                editingContribution?.id === contribution.id ? (
                  /* Inline Edit Form for Completed Investment */
                  <div key={contribution.id} ref={investmentEditFormRef} className="p-4 bg-bg-tertiary rounded-lg border border-accent-purple">
                    <h3 className="text-lg font-medium text-text-primary mb-4">
                      Edit Investment Contribution
                    </h3>
                    {/* Same edit form content - render handled by React */}
                  </div>
                ) : (
                  <div
                    key={contribution.id}
                    ref={(el) => { if (el) investmentCardRefs.current.set(contribution.id, el); }}
                    className="p-4 rounded-lg border border-green-500/30 border-dashed bg-green-500/5 transition-colors"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <h4 className="text-text-primary font-medium">{contribution.name}</h4>
                          {contribution.category && (
                            <span className="px-2 py-0.5 bg-accent-purple/20 text-accent-purple text-xs rounded-full">
                              {contribution.category}
                            </span>
                          )}
                          <span className="px-2 py-0.5 bg-green-500/20 text-green-400 text-xs rounded-full border border-green-500/30">
                            ✓ Completed
                          </span>
                        </div>
                        <div className="text-text-secondary text-sm mt-1">
                          Invested: {contribution.startDate ? new Date(contribution.startDate).toLocaleDateString() : 'Unknown'}
                          {contribution.sourceAccountName && ` • From: ${contribution.sourceAccountName}`}
                          {contribution.targetAccountName && ` → ${contribution.targetAccountName}`}
                        </div>
                      </div>
                      <div className="text-right mr-4">
                        <div className="text-green-400/70 font-semibold line-through">
                          {formatCurrency(contribution.amount, contribution.currency)}
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => handleDelete(contribution.id)}
                          className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  </div>
                )
              ))}
            </div>
          </div>
        )}
      </div>
    </GlassCard>
  );
}

export function GoalsSettings() {
  const { data: goalsData, isLoading } = useGetFinancialGoalsQuery();
  
  const [createGoal, { isLoading: isCreating }] = useCreateFinancialGoalMutation();
  const [updateGoal] = useUpdateFinancialGoalMutation();
  const [deleteGoal] = useDeleteFinancialGoalMutation();
  
  const [showForm, setShowForm] = useState(false);
  const [editingGoal, setEditingGoal] = useState<FinancialGoal | null>(null);
  
  // Refs for auto-scrolling goal forms
  const goalEditFormRef = useRef<HTMLDivElement>(null);
  const newGoalFormRef = useRef<HTMLDivElement>(null);
  const goalCardRefs = useRef<Map<string, HTMLDivElement>>(new Map());
  const [lastSavedGoalId, setLastSavedGoalId] = useState<string | null>(null);
  
  // Scroll to saved item card
  useEffect(() => {
    if (lastSavedGoalId) {
      setTimeout(() => {
        const cardElement = goalCardRefs.current.get(lastSavedGoalId);
        if (cardElement) {
          cardElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        setLastSavedGoalId(null);
      }, 150);
    }
  }, [lastSavedGoalId]);
  
  // Auto-scroll to goal edit form
  useEffect(() => {
    if (editingGoal && goalEditFormRef.current) {
      setTimeout(() => {
        const element = goalEditFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20; // Offset from top of viewport
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [editingGoal]);

  useEffect(() => {
    if (showForm && !editingGoal && newGoalFormRef.current) {
      setTimeout(() => {
        const element = newGoalFormRef.current;
        if (element) {
          const rect = element.getBoundingClientRect();
          const offset = 20; // Offset from top of viewport
          window.scrollTo({
            top: window.scrollY + rect.top - offset,
            behavior: 'smooth'
          });
        }
      }, 100);
    }
  }, [showForm, editingGoal]);

  const [formData, setFormData] = useState<CreateFinancialGoalRequest>({
    name: '',
    targetAmount: 0,
    currentAmount: 0,
    priority: 1,
    category: '',
    iconName: '🎯',
    currency: 'ZAR',
  });

  const goals = goalsData?.goals || [];
  const summary = goalsData?.summary;

  const categoryOptions = [
    'Retirement',
    'Real Estate',
    'Luxury',
    'Experience',
    'Education',
    'Emergency',
    'Other',
  ];

  const iconOptions = ['🎯', '🏎️', '🏠', '💰', '✈️', '🎓', '🛡️', '💎', '🏖️', '🚀'];

  const resetForm = () => {
    setFormData({
      name: '',
      targetAmount: 0,
      currentAmount: 0,
      priority: 1,
      category: '',
      iconName: '🎯',
      currency: 'ZAR',
    });
    setEditingGoal(null);
    setShowForm(false);
  };

  const handleEdit = (goal: FinancialGoal) => {
    setEditingGoal(goal);
    // Convert ISO date to YYYY-MM-DD format for the date input
    const targetDateForInput = goal.targetDate 
      ? goal.targetDate.split('T')[0] 
      : undefined;
    setFormData({
      name: goal.name,
      targetAmount: goal.targetAmount,
      currentAmount: goal.currentAmount,
      priority: goal.priority,
      targetDate: targetDateForInput,
      category: goal.category || '',
      iconName: goal.iconName || '🎯',
      currency: goal.currency,
      notes: goal.notes,
    });
    setShowForm(true);
  };

  const handleSubmit = async () => {
    try {
      const savedId = editingGoal?.id;
      if (editingGoal) {
        await updateGoal({
          id: editingGoal.id,
          ...formData,
        }).unwrap();
      } else {
        await createGoal(formData).unwrap();
      }
      resetForm();
      // Scroll to the saved item card after edit
      if (savedId) {
        setLastSavedGoalId(savedId);
      }
    } catch (err) {
      console.error('Failed to save financial goal:', err);
    }
  };

  const handleDelete = async (id: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to delete this goal?',
    });
    if (confirmed) {
      await deleteGoal(id);
    }
  };

  const formatCurrency = (amount: number, currency: string = 'ZAR') => {
    const symbols: Record<string, string> = { ZAR: 'R', USD: '$', EUR: '€', GBP: '£' };
    return `${symbols[currency] || currency} ${amount.toLocaleString()}`;
  };

  const formatMonths = (months: number | undefined) => {
    if (!months) return 'N/A';
    const years = Math.floor(months / 12);
    const remainingMonths = months % 12;
    if (years === 0) return `${remainingMonths} months`;
    if (remainingMonths === 0) return `${years} years`;
    return `${years}y ${remainingMonths}m`;
  };

  if (isLoading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex justify-center py-8">
          <Loader2 className="w-6 h-6 animate-spin text-accent-purple" />
        </div>
      </GlassCard>
    );
  }

  return (
    <GlassCard variant="default" className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-xl font-semibold text-text-primary">Financial Goals</h2>
          <p className="text-text-secondary text-sm mt-1">
            Set targets and track your progress. Time to acquire is calculated based on your monthly investment rate.
          </p>
        </div>
        <div className="text-right">
          <div className="text-sm text-text-secondary">Monthly Investment Rate</div>
          <div className="text-xl font-semibold text-accent-purple">
            {formatCurrency(summary?.monthlyInvestmentRate || 0)}
          </div>
        </div>
      </div>

      {/* Progress Summary */}
      {summary && summary.totalTargetAmount > 0 && (
        <div className="mb-6 p-4 bg-bg-tertiary rounded-lg border border-glass-border">
          <h3 className="text-sm font-medium text-text-primary mb-3">Overall Progress</h3>
          <div className="mb-3">
            <div className="flex justify-between text-sm mb-1">
              <span className="text-text-secondary">
                {formatCurrency(summary.totalCurrentAmount)} / {formatCurrency(summary.totalTargetAmount)}
              </span>
              <span className="text-accent-purple font-medium">
                {summary.overallProgressPercent.toFixed(1)}%
              </span>
            </div>
            <div className="h-3 bg-background-primary rounded-full overflow-hidden">
              <div 
                className="h-full bg-gradient-to-r from-accent-purple to-accent-cyan transition-all duration-500"
                style={{ width: `${Math.min(summary.overallProgressPercent, 100)}%` }}
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <div className="text-text-secondary">Remaining</div>
              <div className="text-red-400 font-medium">{formatCurrency(summary.totalRemainingAmount)}</div>
            </div>
            <div>
              <div className="text-text-secondary">Est. Time (all goals)</div>
              <div className="text-accent-cyan font-medium">
                {formatMonths(summary.estimatedTotalMonths)}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Add Button */}
      <div className="flex justify-end mb-4">
        <button
          onClick={() => { resetForm(); setShowForm(true); }}
          className="px-4 py-2 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors flex items-center gap-2"
        >
          <Plus className="w-4 h-4" />
          Add Goal
        </button>
      </div>

      {/* Form - Only shown when adding NEW (not editing) */}
      {showForm && !editingGoal && (
        <div ref={newGoalFormRef} className="mb-6 p-4 bg-bg-tertiary rounded-lg border border-glass-border">
          <h3 className="text-lg font-medium text-text-primary mb-4">
            New Financial Goal
          </h3>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">Name *</label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                placeholder="e.g., Future Capital, Dream Car"
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">Category</label>
              <select
                value={formData.category || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, category: e.target.value }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              >
                <option value="">Select category...</option>
                {categoryOptions.map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Target Amount *
                  <InfoTooltip content={TOOLTIP_CONTENT.currency.general} />
                </span>
              </label>
              <div className="flex gap-2">
                <select
                  value={formData.currency}
                  onChange={(e) => setFormData(prev => ({ ...prev, currency: e.target.value }))}
                  className="bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                >
                  <option value="ZAR">ZAR</option>
                  <option value="USD">USD</option>
                  <option value="EUR">EUR</option>
                </select>
                <input
                  type="number"
                  value={formData.targetAmount || ''}
                  onChange={(e) => setFormData(prev => ({ ...prev, targetAmount: parseFloat(e.target.value) || 0 }))}
                  placeholder="0.00"
                  className="flex-1 bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">Current Progress</label>
              <input
                type="number"
                value={formData.currentAmount || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, currentAmount: parseFloat(e.target.value) || 0 }))}
                placeholder="0.00"
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Priority
                  <InfoTooltip content={TOOLTIP_CONTENT.goal.priority} />
                </span>
              </label>
              <select
                value={formData.priority}
                onChange={(e) => setFormData(prev => ({ ...prev, priority: parseInt(e.target.value) }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              >
                <option value={1}>1 - Highest</option>
                <option value={2}>2 - High</option>
                <option value={3}>3 - Medium</option>
                <option value={4}>4 - Low</option>
                <option value={5}>5 - Lowest</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                <span className="inline-flex items-center gap-1.5">
                  Target Date
                  <InfoTooltip content={TOOLTIP_CONTENT.goal.targetDate} />
                </span>
              </label>
              <input
                type="date"
                value={formData.targetDate || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, targetDate: e.target.value || undefined }))}
                className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
              />
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-text-secondary mb-1">Icon</label>
              <div className="flex flex-wrap gap-2">
                {iconOptions.map(icon => (
                  <button
                    key={icon}
                    type="button"
                    onClick={() => setFormData(prev => ({ ...prev, iconName: icon }))}
                    className={cn(
                      "w-10 h-10 rounded-lg text-xl flex items-center justify-center transition-colors",
                      formData.iconName === icon
                        ? "bg-accent-purple/30 border-2 border-accent-purple"
                        : "bg-background-primary border border-glass-border hover:border-accent-purple/50"
                    )}
                  >
                    {icon}
                  </button>
                ))}
              </div>
            </div>
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium text-text-secondary mb-1">Notes</label>
            <textarea
              value={formData.notes || ''}
              onChange={(e) => setFormData(prev => ({ ...prev, notes: e.target.value }))}
              placeholder="Additional details..."
              rows={2}
              className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
            />
          </div>

          <div className="flex justify-end gap-3">
            <button
              onClick={resetForm}
              className="px-4 py-2 bg-background-primary border border-glass-border rounded-lg text-text-secondary hover:text-text-primary transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleSubmit}
              disabled={isCreating || !formData.name || !formData.targetAmount}
              className="px-4 py-2 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors disabled:opacity-50 flex items-center gap-2"
            >
              {isCreating ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  Create
                </>
              )}
            </button>
          </div>
        </div>
      )}

      {/* Goals List */}
      <div className="space-y-4">
        {goals.length > 0 ? (
          goals.map((goal) => (
            editingGoal?.id === goal.id ? (
              /* Inline Edit Form for Goal */
              <div key={goal.id} ref={goalEditFormRef} className="p-4 bg-bg-tertiary rounded-lg border border-accent-purple">
                <h3 className="text-lg font-medium text-text-primary mb-4">
                  Edit Financial Goal
                </h3>
                
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">Name *</label>
                    <input
                      type="text"
                      value={formData.name}
                      onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                      placeholder="e.g., Future Capital, Dream Car"
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">Category</label>
                    <select
                      value={formData.category || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, category: e.target.value }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    >
                      <option value="">Select category...</option>
                      {categoryOptions.map(cat => (
                        <option key={cat} value={cat}>{cat}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Target Amount *
                        <InfoTooltip content={TOOLTIP_CONTENT.goal.targetAmount} />
                      </span>
                    </label>
                    <div className="flex gap-2">
                      <select
                        value={formData.currency}
                        onChange={(e) => setFormData(prev => ({ ...prev, currency: e.target.value }))}
                        className="bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                      >
                        <option value="ZAR">ZAR</option>
                        <option value="USD">USD</option>
                        <option value="EUR">EUR</option>
                      </select>
                      <input
                        type="number"
                        value={formData.targetAmount || ''}
                        onChange={(e) => setFormData(prev => ({ ...prev, targetAmount: parseFloat(e.target.value) || 0 }))}
                        placeholder="0.00"
                        className="flex-1 bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                      />
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">Current Progress</label>
                    <input
                      type="number"
                      value={formData.currentAmount || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, currentAmount: parseFloat(e.target.value) || 0 }))}
                      placeholder="0.00"
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Priority
                        <InfoTooltip content={TOOLTIP_CONTENT.goal.priority} />
                      </span>
                    </label>
                    <select
                      value={formData.priority}
                      onChange={(e) => setFormData(prev => ({ ...prev, priority: parseInt(e.target.value) }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    >
                      <option value={1}>1 - Highest</option>
                      <option value={2}>2 - High</option>
                      <option value={3}>3 - Medium</option>
                      <option value={4}>4 - Low</option>
                      <option value={5}>5 - Lowest</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">
                      <span className="inline-flex items-center gap-1.5">
                        Target Date
                        <InfoTooltip content={TOOLTIP_CONTENT.goal.targetDate} />
                      </span>
                    </label>
                    <input
                      type="date"
                      value={formData.targetDate || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, targetDate: e.target.value || undefined }))}
                      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                    />
                  </div>
                  <div className="md:col-span-2">
                    <label className="block text-sm font-medium text-text-secondary mb-1">Icon</label>
                    <div className="flex flex-wrap gap-2">
                      {iconOptions.map(icon => (
                        <button
                          key={icon}
                          type="button"
                          onClick={() => setFormData(prev => ({ ...prev, iconName: icon }))}
                          className={cn(
                            "w-10 h-10 rounded-lg text-xl flex items-center justify-center transition-colors",
                            formData.iconName === icon
                              ? "bg-accent-purple/30 border-2 border-accent-purple"
                              : "bg-background-primary border border-glass-border hover:border-accent-purple/50"
                          )}
                        >
                          {icon}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>

                <div className="mb-4">
                  <label className="block text-sm font-medium text-text-secondary mb-1">Notes</label>
                  <textarea
                    value={formData.notes || ''}
                    onChange={(e) => setFormData(prev => ({ ...prev, notes: e.target.value }))}
                    placeholder="Additional details..."
                    rows={2}
                    className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
                  />
                </div>

                <div className="flex justify-end gap-3">
                  <button
                    onClick={resetForm}
                    className="px-4 py-2 bg-background-primary border border-glass-border rounded-lg text-text-secondary hover:text-text-primary transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleSubmit}
                    disabled={isCreating || !formData.name || !formData.targetAmount}
                    className="px-4 py-2 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors disabled:opacity-50 flex items-center gap-2"
                  >
                    {isCreating ? (
                      <>
                        <Loader2 className="w-4 h-4 animate-spin" />
                        Saving...
                      </>
                    ) : (
                      <>
                        <Check className="w-4 h-4" />
                        Update
                      </>
                    )}
                  </button>
                </div>
              </div>
            ) : (
              /* Normal Goal Row */
              <div
                key={goal.id}
                ref={(el) => { if (el) goalCardRefs.current.set(goal.id, el); }}
                className={cn(
                "p-4 rounded-lg border transition-colors",
                goal.isActive
                  ? "bg-bg-tertiary border-glass-border"
                  : "bg-bg-tertiary/50 border-glass-border/50 opacity-60"
              )}
            >
              <div className="flex items-start gap-4">
                <div className="text-3xl">{goal.iconName || '🎯'}</div>
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <h4 className="text-text-primary font-medium text-lg">{goal.name}</h4>
                    {goal.category && (
                      <span className="px-2 py-0.5 bg-accent-purple/20 text-accent-purple text-xs rounded-full">
                        {goal.category}
                      </span>
                    )}
                    <span className="px-2 py-0.5 bg-blue-500/20 text-blue-400 text-xs rounded-full">
                      Priority {goal.priority}
                    </span>
                  </div>
                  
                  {/* Progress Bar */}
                  <div className="mb-2">
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-text-secondary">
                        {formatCurrency(goal.currentAmount, goal.currency)} / {formatCurrency(goal.targetAmount, goal.currency)}
                      </span>
                      <span className="text-accent-purple font-medium">
                        {goal.progressPercent.toFixed(1)}%
                      </span>
                    </div>
                    <div className="h-2 bg-background-primary rounded-full overflow-hidden">
                      <div 
                        className="h-full bg-gradient-to-r from-accent-purple to-accent-cyan transition-all duration-500"
                        style={{ width: `${Math.min(goal.progressPercent, 100)}%` }}
                      />
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-4 text-sm text-text-secondary">
                    <span>
                      Remaining: <span className="text-red-400 font-medium">{formatCurrency(goal.remainingAmount, goal.currency)}</span>
                    </span>
                    <span>
                      Time to acquire: <span className="text-accent-cyan font-medium">{formatMonths(goal.monthsToAcquire)}</span>
                    </span>
                    {goal.targetDate && (
                      <span>
                        Target: <span className="text-text-primary">{new Date(goal.targetDate).toLocaleDateString()}</span>
                      </span>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => handleEdit(goal)}
                    className="p-2 text-text-secondary hover:text-text-primary hover:bg-background-hover rounded transition-colors"
                  >
                    <Target className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => handleDelete(goal.id)}
                    className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </div>
            )
          ))
        ) : (
          <div className="text-center py-8 text-text-secondary">
            <Target className="w-12 h-12 mx-auto mb-3 opacity-50" />
            <p>No financial goals configured yet.</p>
            <p className="text-sm">Set your financial goals to track progress and estimate acquisition timelines.</p>
          </div>
        )}
      </div>
    </GlassCard>
  );
}


export function DataPortabilitySettings() {
  const [importDataFile, { isLoading: isImporting }] = useImportDataFileMutation();
  
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [importMode, setImportMode] = useState<"replace" | "merge">("replace");
  const [dryRun, setDryRun] = useState(true);
  const [lastExport, setLastExport] = useState<{ date: string; count: number } | null>(null);
  const [importResult, setImportResult] = useState<{
    success: boolean;
    message: string;
    details?: Record<string, { imported: number; skipped: number; errors: number }>;
  } | null>(null);
  const [exportSuccess, setExportSuccess] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);
  const [isDragging, setIsDragging] = useState(false);

  const handleExport = async () => {
    try {
      setIsExporting(true);
      setExportError(null);
      
      // Get the auth token
      const token = localStorage.getItem('accessToken');
      if (!token) {
        setExportError("No auth token found");
        return;
      }

      // Fetch the file directly from the API
      const response = await fetch('/api/v1/data/export', {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('Export failed');
      }

      // Get the filename from Content-Disposition header or use default
      const contentDisposition = response.headers.get('Content-Disposition');
      let fileName = `lifeos-backup-${new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19)}.json`;
      if (contentDisposition) {
        const match = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
        if (match && match[1]) {
          fileName = match[1].replace(/['"]/g, '');
        }
      }

      // Create blob from response and download
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      // Parse the JSON to get entity count for display
      const text = await blob.text();
      const data = JSON.parse(text);
      
      setLastExport({
        date: new Date().toLocaleString(),
        count: data.meta?.totalEntities || 0,
      });
      setExportSuccess(true);
      setTimeout(() => setExportSuccess(false), 3000);
    } catch (err) {
      console.error("Export failed:", err);
      setExportError(err instanceof Error ? err.message : "Export failed");
    } finally {
      setIsExporting(false);
    }
  };

  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    await processFile(file);

    // Reset file input
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const processFile = async (file: File) => {
    try {
      const result = await importDataFile({
        file,
        mode: importMode,
        dryRun,
      }).unwrap();

      setImportResult({
        success: result.data.status === "completed",
        message: dryRun
          ? `Dry run completed: ${result.data.totalImported} would be imported, ${result.data.totalSkipped} skipped, ${result.data.totalErrors} errors`
          : `Import completed: ${result.data.totalImported} imported, ${result.data.totalSkipped} skipped, ${result.data.totalErrors} errors`,
        details: result.data.results,
      });
    } catch (err: unknown) {
      const error = err as { data?: { error?: { message?: string } } };
      setImportResult({
        success: false,
        message: error?.data?.error?.message || "Import failed",
      });
    }
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  };

  const handleDrop = async (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);

    const files = e.dataTransfer.files;
    if (files && files.length > 0) {
      const file = files[0];
      if (file.type === 'application/json' || file.name.endsWith('.json')) {
        await processFile(file);
      } else {
        setImportResult({
          success: false,
          message: "Please upload a JSON file",
        });
      }
    }
  };

  return (
    <GlassCard variant="default" className="p-6">
      <h2 className="text-xl font-semibold text-text-primary mb-6">Data Portability</h2>
      <p className="text-text-secondary mb-6">
        Export all your data to a JSON file for backup or transfer to another instance.
        Import data from a previous export to restore your data.
      </p>

      {/* Export Section */}
      <div className="mb-8 p-4 bg-bg-tertiary rounded-lg border border-glass-border">
        <h3 className="text-lg font-medium text-text-primary mb-3 flex items-center gap-2">
          <Download className="w-5 h-5 text-accent-purple" />
          Export Data
        </h3>
        <p className="text-text-secondary text-sm mb-4">
          Download all your data including accounts, transactions, metrics, goals, and simulations.
          Sensitive data like passwords and API keys are excluded.
        </p>
        
        <button
          onClick={handleExport}
          disabled={isExporting}
          className="px-4 py-2 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors flex items-center gap-2 disabled:opacity-50"
        >
          {isExporting ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              Exporting...
            </>
          ) : exportSuccess ? (
            <>
              <Check className="w-4 h-4" />
              Exported!
            </>
          ) : (
            <>
              <Download className="w-4 h-4" />
              Export All Data
            </>
          )}
        </button>
        
        {lastExport && (
          <p className="text-text-secondary text-xs mt-2">
            Last export: {lastExport.date} ({lastExport.count} entities)
          </p>
        )}
        
        {exportError && (
          <div className="mt-3 flex items-center gap-2 text-red-400 text-sm">
            <AlertCircle className="w-4 h-4" />
            Export failed. Please try again.
          </div>
        )}
      </div>

      {/* Import Section */}
      <div className="p-4 bg-bg-tertiary rounded-lg border border-glass-border">
        <h3 className="text-lg font-medium text-text-primary mb-3 flex items-center gap-2">
          <Upload className="w-5 h-5 text-accent-cyan" />
          Import Data
        </h3>
        <p className="text-text-secondary text-sm mb-4">
          Restore data from a previously exported JSON file. Click or drag & drop to upload.
        </p>

        <div className="space-y-4 mb-4">
          <div>
            <label className="block text-sm font-medium text-text-secondary mb-2">Import Mode</label>
            <div className="flex gap-4">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name="importMode"
                  checked={importMode === "replace"}
                  onChange={() => setImportMode("replace")}
                  className="w-4 h-4 accent-accent-purple"
                />
                <span className="text-text-primary text-sm">Replace (delete existing, import new)</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name="importMode"
                  checked={importMode === "merge"}
                  onChange={() => setImportMode("merge")}
                  className="w-4 h-4 accent-accent-purple"
                />
                <span className="text-text-primary text-sm">Merge (skip existing, add new)</span>
              </label>
            </div>
          </div>

          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={dryRun}
              onChange={(e) => setDryRun(e.target.checked)}
              className="w-4 h-4 accent-accent-purple"
            />
            <span className="text-text-primary text-sm">
              Dry Run (preview changes without applying)
            </span>
          </label>
        </div>

        <input
          ref={fileInputRef}
          type="file"
          accept=".json,application/json"
          onChange={handleFileSelect}
          className="hidden"
          id="file-upload-input"
        />

        {/* Drag & Drop Zone */}
        <div
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
          className={cn(
            "relative border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-all",
            isDragging
              ? "border-accent-cyan bg-accent-cyan/10 scale-[1.02]"
              : "border-glass-border hover:border-accent-cyan/50 hover:bg-bg-tertiary/50",
            isImporting && "pointer-events-none opacity-50"
          )}
        >
          <div className="flex flex-col items-center gap-3">
            <div className={cn(
              "p-4 rounded-full transition-colors",
              isDragging ? "bg-accent-cyan/20" : "bg-bg-tertiary"
            )}>
              <Upload className={cn(
                "w-8 h-8 transition-colors",
                isDragging ? "text-accent-cyan" : "text-text-secondary"
              )} />
            </div>
            <div>
              <p className="text-text-primary font-medium mb-1">
                {isImporting ? "Importing..." : isDragging ? "Drop file here" : "Click to select or drag & drop"}
              </p>
              <p className="text-text-secondary text-sm">
                JSON files only
              </p>
            </div>
            {isImporting && (
              <Loader2 className="w-6 h-6 animate-spin text-accent-cyan" />
            )}
          </div>
        </div>

        {importResult && (
          <div
            className={cn(
              "mt-4 p-3 rounded-lg border",
              importResult.success
                ? "bg-green-500/10 border-green-500/30"
                : "bg-red-500/10 border-red-500/30"
            )}
          >
            <p className={importResult.success ? "text-green-400" : "text-red-400"}>
              {importResult.message}
            </p>
            {importResult.details && (
              <div className="mt-2 text-xs text-text-secondary space-y-1 max-h-32 overflow-y-auto">
                {Object.entries(importResult.details).map(([entity, stats]) => (
                  <div key={entity} className="flex justify-between">
                    <span>{entity}:</span>
                    <span>
                      {stats.imported} imported, {stats.skipped} skipped
                      {stats.errors > 0 && <span className="text-red-400">, {stats.errors} errors</span>}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {importMode === "replace" && !dryRun && (
          <div className="mt-3 flex items-center gap-2 text-yellow-400 text-sm">
            <AlertCircle className="w-4 h-4" />
            Warning: Replace mode will delete all existing data before importing!
          </div>
        )}
      </div>
    </GlassCard>
  );
}

