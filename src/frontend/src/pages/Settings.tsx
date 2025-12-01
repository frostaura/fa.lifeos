import { useState, useEffect, useCallback, useRef } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { InfoTooltip } from '@components/atoms/InfoTooltip';
import { User, Key, Grid3X3, Calculator, DollarSign, Copy, Trash2, Plus, Check, Loader2, AlertCircle, TrendingUp, Target, Download, Upload } from 'lucide-react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { cn } from '@utils/cn';
import { TOOLTIP_CONTENT } from '@utils/tooltipContent';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from 'recharts';
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
  useLazyExportDataQuery,
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
} from '@/types';

const settingsNav = [
  { icon: User, label: 'Profile', path: '/settings/profile' },
  { icon: Key, label: 'API Keys', path: '/settings/api-keys' },
  { icon: Grid3X3, label: 'Dimensions', path: '/settings/dimensions' },
  { icon: Calculator, label: 'Tax Profiles', path: '/settings/tax-profiles' },
  { icon: DollarSign, label: 'Income/Expenses', path: '/settings/income-expenses' },
  { icon: TrendingUp, label: 'Investments', path: '/settings/investments' },
  { icon: Target, label: 'Goals', path: '/settings/goals' },
  { icon: Download, label: 'Data Portability', path: '/settings/data' },
];

export function Settings() {
  const location = useLocation();
  const isRootSettings = location.pathname === '/settings' || location.pathname === '/settings/';

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-text-primary">Settings</h1>
        <p className="text-text-secondary mt-1">Manage your preferences</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Settings Navigation */}
        <GlassCard variant="default" className="p-4 h-fit">
          <nav className="space-y-1">
            {settingsNav.map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors',
                    isActive
                      ? 'bg-accent-purple/20 text-accent-purple'
                      : 'text-text-secondary hover:text-text-primary hover:bg-background-hover'
                  )
                }
              >
                <item.icon className="w-5 h-5" />
                <span className="font-medium">{item.label}</span>
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
            className="w-full bg-background-tertiary/50 border border-glass-border rounded-lg px-4 py-2.5 text-text-secondary cursor-not-allowed"
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
            className="w-full bg-background-tertiary border border-glass-border rounded-lg px-4 py-2.5 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
            className="w-full bg-background-tertiary border border-glass-border rounded-lg px-4 py-2.5 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
            className="w-full bg-background-tertiary border border-glass-border rounded-lg px-4 py-2.5 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
    if (confirm('Are you sure you want to revoke this API key? This cannot be undone.')) {
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
          className="flex-1 bg-background-tertiary border border-glass-border rounded-lg px-4 py-2.5 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
            <code className="flex-1 bg-background-tertiary px-3 py-2 rounded text-text-primary text-sm font-mono overflow-x-auto">
              {newKey}
            </code>
            <button
              onClick={handleCopyKey}
              className="p-2 bg-background-tertiary rounded hover:bg-background-hover transition-colors"
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
                className="flex items-center justify-between p-3 bg-background-tertiary rounded-lg"
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
            className="p-4 bg-background-tertiary rounded-lg flex items-center gap-4"
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
    if (confirm('Are you sure you want to delete this tax profile?')) {
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
        <div className="mb-6 p-4 bg-background-tertiary rounded-lg border border-glass-border">
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                        className="w-full bg-background-tertiary border border-glass-border rounded px-2 py-1 text-text-primary text-sm"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-text-secondary">Max</label>
                      <input
                        type="number"
                        value={bracket.max || ''}
                        onChange={(e) => handleBracketChange(index, 'max', e.target.value ? parseFloat(e.target.value) : null)}
                        placeholder="∞"
                        className="w-full bg-background-tertiary border border-glass-border rounded px-2 py-1 text-text-primary text-sm"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-text-secondary">Rate %</label>
                      <input
                        type="number"
                        step="0.01"
                        value={(bracket.rate * 100).toFixed(0)}
                        onChange={(e) => handleBracketChange(index, 'rate', parseFloat(e.target.value) / 100)}
                        className="w-full bg-background-tertiary border border-glass-border rounded px-2 py-1 text-text-primary text-sm"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-text-secondary">Base Tax</label>
                      <input
                        type="number"
                        value={bracket.baseTax}
                        onChange={(e) => handleBracketChange(index, 'baseTax', parseFloat(e.target.value))}
                        className="w-full bg-background-tertiary border border-glass-border rounded px-2 py-1 text-text-primary text-sm"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                  className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary text-sm focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                  className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary text-sm focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                  className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary text-sm focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                  : "bg-background-tertiary border-glass-border"
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
  
  // Income form state
  const [incomeForm, setIncomeForm] = useState<CreateIncomeSourceRequest>({
    name: '',
    currency: 'ZAR',
    baseAmount: 0,
    isPreTax: true,
    paymentFrequency: 'Monthly',
    employerName: '',
    notes: '',
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
  });

  const frequencyOptions = [
    { value: 'Monthly', label: 'Monthly' },
    { value: 'Annual', label: 'Annually' },
    { value: 'Weekly', label: 'Weekly' },
    { value: 'BiWeekly', label: 'Bi-Weekly' },
  ];

  const expenseCategories = [
    'Housing', 'Transport', 'Food', 'Utilities', 'Insurance', 
    'Healthcare', 'Entertainment', 'Education', 'Savings', 'Debt', 'Other'
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
      category: 'Other',
      isTaxDeductible: false,
      inflationAdjusted: true,
    });
    setEditingExpense(null);
    setShowExpenseForm(false);
  };

  const handleEditIncome = (income: IncomeSource) => {
    setEditingIncome(income);
    setIncomeForm({
      name: income.name,
      currency: income.currency,
      baseAmount: income.baseAmount,
      isPreTax: income.isPreTax,
      taxProfileId: income.taxProfileId,
      paymentFrequency: income.paymentFrequency,
      annualIncreaseRate: income.annualIncreaseRate,
      employerName: income.employerName || '',
      notes: income.notes || '',
    });
    setShowIncomeForm(true);
  };

  const handleEditExpense = (expense: ExpenseDefinition) => {
    setEditingExpense(expense);
    setExpenseForm({
      name: expense.name,
      currency: expense.currency,
      amountType: expense.amountType,
      amountValue: expense.amountValue,
      amountFormula: expense.amountFormula,
      frequency: expense.frequency,
      category: expense.category,
      isTaxDeductible: expense.isTaxDeductible,
      inflationAdjusted: expense.inflationAdjusted,
    });
    setShowExpenseForm(true);
  };

  const handleSubmitIncome = async () => {
    try {
      if (editingIncome) {
        const clearTaxProfile = !incomeForm.taxProfileId && editingIncome.taxProfileId ? true : false;
        await updateIncomeSource({
          id: editingIncome.id,
          ...incomeForm,
          clearTaxProfile,
        }).unwrap();
      } else {
        await createIncomeSource(incomeForm).unwrap();
      }
      resetIncomeForm();
    } catch (err) {
      console.error('Failed to save income source:', err);
    }
  };

  const handleSubmitExpense = async () => {
    try {
      if (editingExpense) {
        await updateExpenseDefinition({
          id: editingExpense.id,
          ...expenseForm,
        }).unwrap();
      } else {
        await createExpenseDefinition(expenseForm).unwrap();
      }
      resetExpenseForm();
    } catch (err) {
      console.error('Failed to save expense definition:', err);
    }
  };

  const handleDeleteIncome = async (id: string) => {
    if (confirm('Are you sure you want to delete this income source?')) {
      await deleteIncomeSource(id);
    }
  };

  const handleDeleteExpense = async (id: string) => {
    if (confirm('Are you sure you want to delete this expense?')) {
      await deleteExpenseDefinition(id);
    }
  };

  const formatCurrency = (amount: number, currency: string = 'ZAR') => {
    const symbols: Record<string, string> = { ZAR: 'R', USD: '$', EUR: '€', GBP: '£' };
    return `${symbols[currency] || currency} ${amount.toLocaleString()}`;
  };

  const getMonthlyAmount = (amount: number, frequency: string) => {
    switch (frequency) {
      case 'Annual': return amount / 12;
      case 'Weekly': return amount * 4.33;
      case 'BiWeekly': return amount * 2.17;
      default: return amount;
    }
  };

  // Use API-calculated values when available, fallback to local calculation
  const totalMonthlyGross = incomeSummary?.totalMonthlyGross ?? (incomeSources?.filter(i => i.isActive).reduce(
    (sum, i) => sum + getMonthlyAmount(i.baseAmount, i.paymentFrequency), 0
  ) || 0);
  
  const totalMonthlyTax = incomeSummary?.totalMonthlyTax || 0;
  const totalMonthlyUif = incomeSummary?.totalMonthlyUif || 0;
  const totalMonthlyNet = incomeSummary?.totalMonthlyNet ?? (totalMonthlyGross - totalMonthlyTax - totalMonthlyUif);

  const totalMonthlyExpenses = expenseDefinitions?.filter(e => e.isActive).reduce(
    (sum, e) => sum + getMonthlyAmount(e.amountValue || 0, e.frequency), 0
  ) || 0;
  
  const totalMonthlyInvestments = investmentData?.summary?.totalMonthlyContributions || 0;
  
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
        <div className="mb-6 p-4 bg-background-tertiary rounded-lg border border-glass-border">
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
        <div className="mb-6 p-4 bg-background-tertiary rounded-lg border border-glass-border">
          <h3 className="text-sm font-medium text-text-primary mb-4">Monthly Financial Breakdown</h3>
          <div className="flex flex-col md:flex-row items-center gap-6">
            <div className="w-full md:w-1/2 h-64">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={pieChartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={100}
                    paddingAngle={2}
                    dataKey="value"
                  >
                    {pieChartData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip
                    content={({ active, payload }) => {
                      if (!active || !payload?.length) return null;
                      const item = payload[0].payload;
                      const percent = ((item.value / totalMonthlyGross) * 100).toFixed(1);
                      return (
                        <div className="bg-background-tertiary border border-glass-border rounded-lg p-3 shadow-lg">
                          <p className="text-text-primary font-medium">{item.name}</p>
                          <p className="text-text-secondary">{formatCurrency(item.value)} ({percent}%)</p>
                        </div>
                      );
                    }}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="flex-1 space-y-2">
              {pieChartData.map((item, index) => (
                <div key={index} className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <div 
                      className="w-3 h-3 rounded-full" 
                      style={{ backgroundColor: item.color }}
                    />
                    <span className="text-text-primary text-sm">{item.name}</span>
                  </div>
                  <div className="text-right">
                    <span className="text-text-secondary text-sm">{formatCurrency(item.value)}</span>
                    <span className="text-text-tertiary text-xs ml-2">
                      ({((item.value / totalMonthlyGross) * 100).toFixed(1)}%)
                    </span>
                  </div>
                </div>
              ))}
              <div className="border-t border-glass-border pt-2 mt-2">
                <div className="flex items-center justify-between font-medium">
                  <span className="text-text-primary text-sm">Total Gross</span>
                  <span className="text-green-400">{formatCurrency(totalMonthlyGross)}</span>
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
            <button
              onClick={() => { resetIncomeForm(); setShowIncomeForm(true); }}
              className="px-4 py-2 bg-green-600 rounded-lg text-white font-medium hover:bg-green-700 transition-colors flex items-center gap-2"
            >
              <Plus className="w-4 h-4" />
              Add Income
            </button>
          </div>

          {/* Income Form - Only shown when adding NEW (not editing) */}
          {showIncomeForm && !editingIncome && (
            <div className="p-4 bg-background-tertiary rounded-lg border border-glass-border">
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
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-secondary mb-1">Employer/Source</label>
                  <input
                    type="text"
                    value={incomeForm.employerName || ''}
                    onChange={(e) => setIncomeForm(prev => ({ ...prev, employerName: e.target.value }))}
                    placeholder="e.g., Company Name"
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                      className="bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                      className="flex-1 bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
                  >
                    {frequencyOptions.map(opt => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
                </div>
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
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
                  />
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
                  className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                  disabled={isCreatingIncome || !incomeForm.name || !incomeForm.baseAmount}
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
            {incomeSources && incomeSources.length > 0 ? (
              incomeSources.map((income) => (
                editingIncome?.id === income.id ? (
                  /* Inline Edit Form */
                  <div key={income.id} className="p-4 bg-background-tertiary rounded-lg border border-accent-purple">
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
                          className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-text-secondary mb-1">Employer/Source</label>
                        <input
                          type="text"
                          value={incomeForm.employerName || ''}
                          onChange={(e) => setIncomeForm(prev => ({ ...prev, employerName: e.target.value }))}
                          placeholder="e.g., Company Name"
                          className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                            className="bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                            className="flex-1 bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                          className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
                        >
                          {frequencyOptions.map(opt => (
                            <option key={opt.value} value={opt.value}>{opt.label}</option>
                          ))}
                        </select>
                      </div>
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
                          className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                          className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
                        />
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
                        className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-green-500"
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
                        disabled={isCreatingIncome || !incomeForm.name || !incomeForm.baseAmount}
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
                    className={cn(
                    "p-4 rounded-lg border transition-colors",
                    income.isActive
                      ? "bg-background-tertiary border-glass-border"
                      : "bg-background-tertiary/50 border-glass-border/50 opacity-60"
                  )}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h4 className="text-text-primary font-medium">{income.name}</h4>
                        {!income.isActive && (
                          <span className="px-2 py-0.5 bg-gray-600/20 text-gray-400 text-xs rounded-full">
                            Inactive
                          </span>
                        )}
                      </div>
                      <div className="text-text-secondary text-sm mt-1">
                        {income.employerName && `${income.employerName} • `}
                        {income.paymentFrequency} • {income.isPreTax ? 'Gross' : 'Net'}
                        {income.annualIncreaseRate && ` • +${(income.annualIncreaseRate * 100).toFixed(1)}%/yr`}
                      </div>
                    </div>
                    <div className="text-right mr-4">
                      <div className="text-green-400 font-semibold">
                        {formatCurrency(income.baseAmount, income.currency)}
                      </div>
                      <div className="text-text-secondary text-xs">
                        ≈ {formatCurrency(getMonthlyAmount(income.baseAmount, income.paymentFrequency), income.currency)}/mo
                      </div>
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
            <button
              onClick={() => { resetExpenseForm(); setShowExpenseForm(true); }}
              className="px-4 py-2 bg-red-600 rounded-lg text-white font-medium hover:bg-red-700 transition-colors flex items-center gap-2"
            >
              <Plus className="w-4 h-4" />
              Add Expense
            </button>
          </div>

          {/* Expense Form - Only shown when adding NEW (not editing) */}
          {showExpenseForm && !editingExpense && (
            <div className="p-4 bg-background-tertiary rounded-lg border border-glass-border">
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
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
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
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
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
                      className="bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
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
                      className="flex-1 bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
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
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
                  >
                    {frequencyOptions.map(opt => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                    <option value="Once">One-Time</option>
                  </select>
                </div>
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
            {expenseDefinitions && expenseDefinitions.length > 0 ? (
              expenseDefinitions.map((expense) => (
                editingExpense?.id === expense.id ? (
                  /* Inline Edit Form for Expense */
                  <div key={expense.id} className="p-4 bg-background-tertiary rounded-lg border border-accent-purple">
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
                          className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
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
                          className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
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
                            className="bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
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
                            className="flex-1 bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
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
                          className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-red-500"
                        >
                          {frequencyOptions.map(opt => (
                            <option key={opt.value} value={opt.value}>{opt.label}</option>
                          ))}
                          <option value="Once">One-Time</option>
                        </select>
                      </div>
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
                    className={cn(
                      "p-4 rounded-lg border transition-colors",
                      expense.isActive
                        ? "bg-background-tertiary border-glass-border"
                        : "bg-background-tertiary/50 border-glass-border/50 opacity-60"
                    )}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <h4 className="text-text-primary font-medium">{expense.name}</h4>
                          <span className="px-2 py-0.5 bg-accent-purple/20 text-accent-purple text-xs rounded-full">
                            {expense.category}
                          </span>
                          {!expense.isActive && (
                            <span className="px-2 py-0.5 bg-gray-600/20 text-gray-400 text-xs rounded-full">
                              Inactive
                            </span>
                          )}
                        </div>
                        <div className="text-text-secondary text-sm mt-1">
                          {expense.frequency}
                          {expense.isTaxDeductible && ' • Tax Deductible'}
                          {expense.inflationAdjusted && ' • Inflation Adj.'}
                        </div>
                      </div>
                      <div className="text-right mr-4">
                        <div className="text-red-400 font-semibold">
                          {formatCurrency(expense.amountValue || 0, expense.currency)}
                        </div>
                        <div className="text-text-secondary text-xs">
                          ≈ {formatCurrency(getMonthlyAmount(expense.amountValue || 0, expense.frequency), expense.currency)}/mo
                        </div>
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
  
  const [formData, setFormData] = useState<CreateInvestmentContributionRequest>({
    name: '',
    currency: 'ZAR',
    amount: 0,
    frequency: 'Monthly',
    category: '',
  });

  const contributions = contributionsData?.sources || [];
  const summary = contributionsData?.summary;

  const frequencyOptions = [
    { value: 'Monthly', label: 'Monthly' },
    { value: 'Annual', label: 'Annually' },
    { value: 'Weekly', label: 'Weekly' },
    { value: 'BiWeekly', label: 'Bi-Weekly' },
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

  const resetForm = () => {
    setFormData({
      name: '',
      currency: 'ZAR',
      amount: 0,
      frequency: 'Monthly',
      category: '',
    });
    setEditingContribution(null);
    setShowForm(false);
  };

  const handleEdit = (contribution: InvestmentContribution) => {
    setEditingContribution(contribution);
    setFormData({
      name: contribution.name,
      currency: contribution.currency,
      amount: contribution.amount,
      frequency: contribution.frequency,
      targetAccountId: contribution.targetAccountId,
      category: contribution.category || '',
      annualIncreaseRate: contribution.annualIncreaseRate,
      notes: contribution.notes,
    });
    setShowForm(true);
  };

  const handleSubmit = async () => {
    try {
      if (editingContribution) {
        await updateContribution({
          id: editingContribution.id,
          ...formData,
        }).unwrap();
      } else {
        await createContribution(formData).unwrap();
      }
      resetForm();
    } catch (err) {
      console.error('Failed to save investment contribution:', err);
    }
  };

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this investment contribution?')) {
      await deleteContribution(id);
    }
  };

  const formatCurrency = (amount: number, currency: string = 'ZAR') => {
    const symbols: Record<string, string> = { ZAR: 'R', USD: '$', EUR: '€', GBP: '£' };
    return `${symbols[currency] || currency} ${amount.toLocaleString()}`;
  };

  const getMonthlyAmount = (amount: number, frequency: string) => {
    switch (frequency) {
      case 'Annual': return amount / 12;
      case 'Weekly': return amount * 4.33;
      case 'BiWeekly': return amount * 2.17;
      default: return amount;
    }
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
        <div className="mb-6 p-4 bg-background-tertiary rounded-lg border border-glass-border">
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
        <div className="mb-6 p-4 bg-background-tertiary rounded-lg border border-glass-border">
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                  className="bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                  className="flex-1 bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
              >
                {frequencyOptions.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
              >
                <option value="">No linked account</option>
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
              className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
              disabled={isCreating || !formData.name || !formData.amount}
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
        {contributions.length > 0 ? (
          contributions.map((contribution) => (
            editingContribution?.id === contribution.id ? (
              /* Inline Edit Form for Investment Contribution */
              <div key={contribution.id} className="p-4 bg-background-tertiary rounded-lg border border-accent-purple">
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
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                        className="bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                        className="flex-1 bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
                    >
                      {frequencyOptions.map(opt => (
                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                      ))}
                    </select>
                  </div>
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
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
                    >
                      <option value="">No linked account</option>
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
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                    disabled={isCreating || !formData.name || !formData.amount}
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
                className={cn(
                  "p-4 rounded-lg border transition-colors",
                  contribution.isActive
                    ? "bg-background-tertiary border-glass-border"
                    : "bg-background-tertiary/50 border-glass-border/50 opacity-60"
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
                    </div>
                    <div className="text-text-secondary text-sm mt-1">
                      {contribution.frequency}
                      {contribution.targetAccountName && ` → ${contribution.targetAccountName}`}
                      {contribution.annualIncreaseRate && ` • +${(contribution.annualIncreaseRate * 100).toFixed(1)}%/yr`}
                    </div>
                  </div>
                  <div className="text-right mr-4">
                    <div className="text-accent-purple font-semibold">
                      {formatCurrency(contribution.amount, contribution.currency)}
                    </div>
                    <div className="text-text-secondary text-xs">
                      ≈ {formatCurrency(getMonthlyAmount(contribution.amount, contribution.frequency), contribution.currency)}/mo
                    </div>
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
    setFormData({
      name: goal.name,
      targetAmount: goal.targetAmount,
      currentAmount: goal.currentAmount,
      priority: goal.priority,
      targetDate: goal.targetDate,
      category: goal.category || '',
      iconName: goal.iconName || '🎯',
      currency: goal.currency,
      notes: goal.notes,
    });
    setShowForm(true);
  };

  const handleSubmit = async () => {
    try {
      if (editingGoal) {
        await updateGoal({
          id: editingGoal.id,
          ...formData,
        }).unwrap();
      } else {
        await createGoal(formData).unwrap();
      }
      resetForm();
    } catch (err) {
      console.error('Failed to save financial goal:', err);
    }
  };

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this goal?')) {
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
        <div className="mb-6 p-4 bg-background-tertiary rounded-lg border border-glass-border">
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
                {formatMonths(summary.monthlyInvestmentRate > 0 
                  ? Math.ceil(summary.totalRemainingAmount / summary.monthlyInvestmentRate)
                  : undefined
                )}
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
        <div className="mb-6 p-4 bg-background-tertiary rounded-lg border border-glass-border">
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">Category</label>
              <select
                value={formData.category || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, category: e.target.value }))}
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                  className="bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                  className="flex-1 bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
              className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
              <div key={goal.id} className="p-4 bg-background-tertiary rounded-lg border border-accent-purple">
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
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-secondary mb-1">Category</label>
                    <select
                      value={formData.category || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, category: e.target.value }))}
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                        className="bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                        className="flex-1 bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                      className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                    className="w-full bg-background-primary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple"
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
                className={cn(
                "p-4 rounded-lg border transition-colors",
                goal.isActive
                  ? "bg-background-tertiary border-glass-border"
                  : "bg-background-tertiary/50 border-glass-border/50 opacity-60"
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
  const [triggerExport, { isLoading: isExporting, error: exportError }] = useLazyExportDataQuery();
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

  const handleExport = async () => {
    try {
      const result = await triggerExport().unwrap();
      
      // Download as JSON file
      const dataStr = JSON.stringify(result, null, 2);
      const blob = new Blob([dataStr], { type: "application/json" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `lifeos-export-${new Date().toISOString().split("T")[0]}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
      
      setLastExport({
        date: new Date().toLocaleString(),
        count: result.meta?.totalEntities || 0,
      });
      setExportSuccess(true);
      setTimeout(() => setExportSuccess(false), 3000);
    } catch (err) {
      console.error("Export failed:", err);
    }
  };

  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

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

    // Reset file input
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
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
      <div className="mb-8 p-4 bg-background-tertiary rounded-lg border border-glass-border">
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
      <div className="p-4 bg-background-tertiary rounded-lg border border-glass-border">
        <h3 className="text-lg font-medium text-text-primary mb-3 flex items-center gap-2">
          <Upload className="w-5 h-5 text-accent-cyan" />
          Import Data
        </h3>
        <p className="text-text-secondary text-sm mb-4">
          Restore data from a previously exported JSON file.
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
        />

        <button
          onClick={() => fileInputRef.current?.click()}
          disabled={isImporting}
          className="px-4 py-2 bg-accent-cyan rounded-lg text-white font-medium hover:bg-accent-cyan/80 transition-colors flex items-center gap-2 disabled:opacity-50"
        >
          {isImporting ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              Importing...
            </>
          ) : (
            <>
              <Upload className="w-4 h-4" />
              Select File to Import
            </>
          )}
        </button>

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

