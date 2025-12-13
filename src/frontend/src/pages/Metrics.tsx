import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import Editor from '@monaco-editor/react';
import toast from 'react-hot-toast';
import { Play, Check, AlertCircle, Clock, RefreshCw, Copy, FileJson, Plus, Edit2, Trash2, Activity, CheckCircle, XCircle, Link2 } from 'lucide-react';
import { cn } from '@utils/cn';
import { confirmToast } from '@utils/confirmToast';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import { Select } from '@components/atoms/Select';
import { Spinner } from '@components/atoms/Spinner';
import {
  useGetMetricDefinitionsQuery,
  useCreateMetricDefinitionMutation,
  useUpdateMetricDefinitionMutation,
  useDeleteMetricDefinitionMutation,
  useGetDimensionsQuery,
  type MetricDefinition,
  type CreateMetricDefinitionRequest,
  type UpdateMetricDefinitionRequest,
} from '@/services';

interface EventLogItem {
  id: string;
  eventType: string;
  source: string;
  status: string;
  timestamp: string;
  requestPayload?: string;
  responsePayload?: string;
  errorMessage?: string;
}

const examplePayload = {
  source: "playground",
  metrics: {
    weight_kg: 75.5,
    sleep_hours: 7.5,
    steps: 10500
  }
};

const jsonSchema = {
  type: "object",
  required: ["metrics"],
  properties: {
    source: {
      type: "string",
      description: "Source of the metrics (e.g., 'n8n', 'ios_shortcuts', 'apple_health')"
    },
    metrics: {
      type: "object",
      description: "Key-value pairs of metric codes and their numeric values",
      additionalProperties: { type: "number" }
    }
  }
};

const valueTypeOptions = [
  { value: 'integer', label: 'Integer' },
  { value: 'decimal', label: 'Decimal' },
  { value: 'boolean', label: 'Boolean' },
  { value: 'percentage', label: 'Percentage' },
];

// Modal for creating/editing metric definitions
interface DefinitionModalProps {
  isOpen: boolean;
  onClose: () => void;
  definition?: MetricDefinition | null;
  onSubmit: (data: CreateMetricDefinitionRequest | (UpdateMetricDefinitionRequest & { code: string })) => Promise<void>;
  isLoading: boolean;
  dimensions: Array<{ id: string; code: string; name: string }>;
}

function DefinitionModal({ isOpen, onClose, definition, onSubmit, isLoading, dimensions }: DefinitionModalProps) {
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [unit, setUnit] = useState('');
  const [valueType, setValueType] = useState<MetricDefinition['valueType']>('decimal');
  const [targetValue, setTargetValue] = useState('');
  const [dimensionId, setDimensionId] = useState<string>('');
  const [isActive, setIsActive] = useState(true);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const isEditing = !!definition;

  // Build dimension options for the select - dimension is mandatory
  const dimensionOptions = dimensions.map((d: any) => ({
    value: d.id,
    label: d.name,
  }));

  useEffect(() => {
    if (definition) {
      setCode(definition.code);
      setName(definition.name);
      setDescription(definition.description || '');
      setUnit(definition.unit);
      setValueType(definition.valueType);
      setTargetValue(definition.targetValue !== undefined ? String(definition.targetValue) : '');
      setDimensionId(definition.dimensionId || '');
      setIsActive(definition.isActive);
    } else {
      setCode('');
      setName('');
      setDescription('');
      setUnit('');
      setValueType('decimal');
      setTargetValue('');
      setDimensionId('');
      setIsActive(true);
    }
    setErrors({});
  }, [definition, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};

    if (!isEditing && !code.trim()) newErrors.code = 'Code is required';
    if (!name.trim()) newErrors.name = 'Name is required';
    if (!unit.trim()) newErrors.unit = 'Unit is required';
    if (!dimensionId) newErrors.dimensionId = 'Dimension is required';
    if (targetValue && isNaN(parseFloat(targetValue))) newErrors.targetValue = 'Must be a number';

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    const parsedTarget = targetValue ? parseFloat(targetValue) : undefined;

    if (isEditing) {
      await onSubmit({
        code: definition!.code,
        name,
        description: description || undefined,
        unit,
        valueType,
        targetValue: parsedTarget,
        dimensionId: dimensionId || undefined,
        isActive,
      });
    } else {
      await onSubmit({
        code,
        name,
        description: description || undefined,
        unit,
        valueType,
        targetValue: parsedTarget,
        dimensionId: dimensionId || undefined,
      });
    }
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center"
      onMouseDown={(e) => {
        mouseDownTargetRef.current = e.target;
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget && mouseDownTargetRef.current === e.target) {
          onClose();
        }
        mouseDownTargetRef.current = null;
      }}
    >
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" />

      <GlassCard variant="elevated" className="relative z-10 w-full max-w-md mx-4 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold text-text-primary">
            {isEditing ? 'Edit Metric Definition' : 'Add Metric Definition'}
          </h2>
          <button onClick={onClose} className="p-2 rounded-lg hover:bg-background-hover transition-colors">
            <XCircle className="w-5 h-5 text-text-tertiary" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {!isEditing && (
            <Input
              label="Code"
              placeholder="e.g., weight_kg"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              error={errors.code}
            />
          )}

          <Input
            label="Name"
            placeholder="e.g., Body Weight"
            value={name}
            onChange={(e) => setName(e.target.value)}
            error={errors.name}
          />

          <Input
            label="Description (Optional)"
            placeholder="e.g., Daily body weight measurement"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />

          <Select
            label="Dimension"
            options={dimensionOptions}
            value={dimensionId}
            onChange={(e) => setDimensionId(e.target.value)}
            error={errors.dimensionId}
          />
          <p className="text-xs text-text-tertiary -mt-2">
            Every metric must be linked to a life dimension.
          </p>

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Unit"
              placeholder="e.g., kg, hours, count"
              value={unit}
              onChange={(e) => setUnit(e.target.value)}
              error={errors.unit}
            />
            <Select
              label="Value Type"
              options={valueTypeOptions}
              value={valueType}
              onChange={(e) => setValueType(e.target.value as MetricDefinition['valueType'])}
            />
          </div>

          <Input
            label="Target Value (Optional)"
            placeholder="e.g., 76 for target weight"
            type="number"
            step="any"
            value={targetValue}
            onChange={(e) => setTargetValue(e.target.value)}
            error={errors.targetValue}
          />
          <p className="text-xs text-text-tertiary -mt-2">
            Set a goal for this metric. It will appear as a reference line in charts.
          </p>

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={isLoading}>
              {isEditing ? 'Update' : 'Create'}
            </Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}

// Modal for editing metric records

// Main Metrics component with both management and playground
export function Metrics() {
  const navigate = useNavigate();
  const token = localStorage.getItem('accessToken');
  
  // Management state
  const { data: definitions, isLoading: isLoadingDefs, error: defsError, refetch: refetchDefs } = useGetMetricDefinitionsQuery();
  const { data: dimensionsData } = useGetDimensionsQuery();
  const [createDefinition, { isLoading: isCreating }] = useCreateMetricDefinitionMutation();
  const [updateDefinition, { isLoading: isUpdatingDef }] = useUpdateMetricDefinitionMutation();
  const [deleteDefinition] = useDeleteMetricDefinitionMutation();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingDefinition, setEditingDefinition] = useState<MetricDefinition | null>(null);
  // Removed selectedDefinition - now using dedicated route

  // Transform dimensions for modal and lookup
  const dimensions = (dimensionsData?.data || []).map((d: any) => ({
    id: d.id,
    code: d.attributes.code,
    name: d.attributes.name,
  }));
  
  // Create dimension lookup map for quick access in table
  const dimensionLookup = new Map(dimensions.map((d: any) => [d.id, d]));
  
  // Playground state
  const [code, setCode] = useState(JSON.stringify(examplePayload, null, 2));
  const [isValid, setIsValid] = useState(true);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [eventLog, setEventLog] = useState<EventLogItem[]>([]);
  const [loadingEvents, setLoadingEvents] = useState(false);
  const [showSchema, setShowSchema] = useState(false);
  const monacoRef = useRef<any>(null);

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  // Management handlers
  const handleCreateOrUpdate = async (
    data: CreateMetricDefinitionRequest | (UpdateMetricDefinitionRequest & { code: string })
  ) => {
    try {
      if ('isActive' in data) {
        await updateDefinition(data as UpdateMetricDefinitionRequest & { code: string }).unwrap();
        toast.success(`Metric "${data.code}" updated successfully`);
      } else {
        await createDefinition(data as CreateMetricDefinitionRequest).unwrap();
        toast.success(`Metric "${data.code}" created successfully`);
      }
      refetchDefs();
    } catch (error: any) {
      const errorMessage = error?.data?.message || error?.message || 'Failed to save metric';
      toast.error(errorMessage);
      throw error;
    }
  };

  const handleDelete = async (defCode: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to delete this metric definition?',
    });
    if (confirmed) {
      try {
        await deleteDefinition(defCode).unwrap();
        toast.success(`Metric "${defCode}" deleted successfully`);
        refetchDefs();
      } catch (error: any) {
        const errorMessage = error?.data?.message || error?.message || 'Failed to delete metric';
        toast.error(errorMessage);
      }
    }
  };

  const openCreateModal = () => {
    setEditingDefinition(null);
    setIsModalOpen(true);
  };

  const openEditModal = (definition: MetricDefinition) => {
    setEditingDefinition(definition);
    setIsModalOpen(true);
  };

  // Validate JSON on change
  useEffect(() => {
    try {
      const parsed = JSON.parse(code);
      
      // Basic schema validation - metrics should be an object with key-value pairs
      if (!parsed.metrics || typeof parsed.metrics !== 'object' || Array.isArray(parsed.metrics)) {
        setIsValid(false);
        setValidationError("'metrics' must be an object with code:value pairs");
        return;
      }
      
      if (Object.keys(parsed.metrics).length === 0) {
        setIsValid(false);
        setValidationError("At least one metric is required");
        return;
      }
      
      for (const [metricCode, value] of Object.entries(parsed.metrics)) {
        if (typeof value !== 'number') {
          setIsValid(false);
          setValidationError(`Value for '${metricCode}' must be a number`);
          return;
        }
      }
      
      setIsValid(true);
      setValidationError(null);
    } catch (e) {
      setIsValid(false);
      setValidationError("Invalid JSON syntax");
    }
  }, [code]);

  // Load event log
  const loadEventLog = async () => {
    setLoadingEvents(true);
    try {
      const response = await fetch('/api/metrics/records?pageSize=20', { headers });
      if (response.ok) {
        const data = await response.json();
        // Transform the records data to event log format
        const events = (data.data || []).map((item: any) => ({
          id: item.id,
          eventType: 'metric_recorded',
          source: item.attributes.source || 'unknown',
          timestamp: item.attributes.recordedAt,
          metricCode: item.attributes.metricCode,
          value: item.attributes.valueNumber,
          status: 'success'
        }));
        setEventLog(events);
      }
    } catch (err) {
      console.error('Failed to load events:', err);
    } finally {
      setLoadingEvents(false);
    }
  };

  useEffect(() => {
    if (token) {
      loadEventLog();
      
      // Restore playground data if user was redirected back after login
      const savedData = sessionStorage.getItem('metricsPlaygroundData');
      if (savedData) {
        setCode(savedData);
        sessionStorage.removeItem('metricsPlaygroundData');
        console.log('[Metrics] Restored playground data after login');
      }
    }
  }, [token]);

  // Update Monaco schema whenever definitions change
  useEffect(() => {
    console.log('[Monaco useEffect] Triggered. monacoRef:', !!monacoRef.current, 'definitions:', definitions?.length || 0);
    if (monacoRef.current && definitions && definitions.length > 0) {
      const metricProperties: Record<string, any> = {};
      
      definitions.forEach((def: any) => {
        metricProperties[def.code] = {
          type: 'number',
          description: `${def.name} (${def.unit || 'no unit'})`
        };
      });

      const schema = {
        type: 'object',
        properties: {
          source: {
            type: 'string',
            description: 'Source of the metrics (e.g., playground, n8n, ios_shortcuts)',
            default: 'playground'
          },
          metrics: {
            type: 'object',
            description: 'Metric values to record',
            properties: metricProperties,
            additionalProperties: false
          }
        },
        required: ['metrics']
      };

      monacoRef.current.languages.json.jsonDefaults.setDiagnosticsOptions({
        validate: true,
        schemas: [{
          uri: 'http://lifeos/metrics-schema.json',
          fileMatch: ['*'],
          schema: schema
        }]
      });
      
      console.log('[Monaco] Updated schema with', Object.keys(metricProperties).length, 'metrics');
    }
  }, [definitions]);

  const handleSend = async () => {
    if (!isValid) return;
    
    setIsLoading(true);
    
    try {
      const payload = JSON.parse(code);
      const response = await fetch('/api/metrics/record', {
        method: 'POST',
        headers,
        body: JSON.stringify(payload),
      });
      
      // Handle 401 - redirect to login while preserving current state
      if (response.status === 401) {
        const currentLocation = { 
          pathname: window.location.pathname, 
          search: window.location.search, 
          hash: window.location.hash 
        };
        
        console.log('[401 Handler] Token expired, storing redirect location and form data:', currentLocation);
        
        // Store the form data so user doesn't lose it
        sessionStorage.setItem('metricsPlaygroundData', code);
        
        // Store current location before redirecting
        sessionStorage.setItem('redirectAfterLogin', JSON.stringify(currentLocation));
        
        // Clear auth data
        localStorage.removeItem('accessToken');
        localStorage.removeItem('user');
        
        // Redirect to login
        console.log('[401 Handler] Redirecting to /login');
        window.location.href = '/login';
        return;
      }
      
      const data = await response.json();
      
      if (response.ok) {
        refetchDefs(); // Refresh definitions to update record counts
        // Refresh event log after successful metric recording
        setTimeout(() => loadEventLog(), 500);
        
        // Show success toast
        const recordedCount = data.data?.attributes?.recorded || 0;
        const failedCount = data.data?.attributes?.failed || 0;
        
        if (failedCount > 0) {
          toast.success(
            `✅ Successfully recorded ${recordedCount} metric${recordedCount !== 1 ? 's' : ''}. ${failedCount} failed.`,
            {
              duration: 4000,
              style: {
                background: '#1a1a2e',
                color: '#fff',
                border: '1px solid rgba(139, 92, 246, 0.3)',
              },
            }
          );
        } else {
          toast.success(
            `✅ Successfully recorded ${recordedCount} metric${recordedCount !== 1 ? 's' : ''}!`,
            {
              duration: 3000,
              style: {
                background: '#1a1a2e',
                color: '#fff',
                border: '1px solid rgba(139, 92, 246, 0.3)',
              },
            }
          );
        }
      } else {
        // Refresh event log on error to show the error event
        setTimeout(() => loadEventLog(), 500);
        
        // Show error toast
        toast.error(
          `❌ Failed to send metrics: ${data.message || data.error?.message || 'Unknown error'}`,
          {
            duration: 4000,
            style: {
              background: '#1a1a2e',
              color: '#fff',
              border: '1px solid rgba(239, 68, 68, 0.3)',
            },
          }
        );
      }
    } catch (err: any) {
      toast.error(
        `❌ Failed to send metrics: ${err.message || 'Unknown error'}`,
        {
          duration: 4000,
          style: {
            background: '#1a1a2e',
            color: '#fff',
            border: '1px solid rgba(239, 68, 68, 0.3)',
          },
        }
      );
    } finally {
      setIsLoading(false);
    }
  };

  const handleReset = () => {
    setCode(JSON.stringify(examplePayload, null, 2));
  };

  const handleCopy = () => {
    navigator.clipboard.writeText(code);
  };

  const formatTimestamp = (ts: string) => {
    const date = new Date(ts);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    
    // If less than 1 minute ago
    if (diffMins < 1) return 'Just now';
    // If less than 60 minutes ago
    if (diffMins < 60) return `${diffMins}m ago`;
    // If less than 24 hours ago
    if (diffHours < 24) return `${diffHours}h ago`;
    // If less than 7 days ago
    if (diffDays < 7) return `${diffDays}d ago`;
    // Otherwise show date
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  };

  // Removed selectedDefinition logic - now using dedicated route /metrics/:code/records

  return (
    <div className="flex flex-col h-full">
      {/* Sticky Header */}
      <div className="flex-shrink-0 sticky top-0 z-20 bg-background-primary/95 backdrop-blur-md border-b border-glass-border rounded-b-xl mb-6">
        <div className="py-4 flex items-center justify-between">
          <div>
            <h1 className="text-xl md:text-2xl lg:text-3xl font-bold text-text-primary">Metrics</h1>
            <p className="text-text-secondary mt-1 text-xs md:text-sm">Manage metric definitions and test the collection API</p>
          </div>
          <Button onClick={openCreateModal} icon={<Plus className="w-4 h-4" />}>
            Add Definition
          </Button>
        </div>
      </div>

      <div className="flex-1 flex flex-col gap-6 px-4 md:px-6 pb-8 min-h-0">

      {/* Quick Add section removed - all metrics come from definitions */}

      {/* Definitions Table - Top Half */}
      <GlassCard variant="default" className="p-6 flex-shrink-0">
        <div className="flex items-center gap-3 mb-4">
          <Activity className="w-5 h-5 text-accent-purple" />
          <h2 className="text-lg font-semibold text-text-primary">Metric Definitions</h2>
          <span className="text-sm text-text-tertiary ml-auto">{definitions?.length || 0} definitions</span>
        </div>

        {isLoadingDefs ? (
          <div className="flex items-center justify-center h-32">
            <Spinner size="lg" />
          </div>
        ) : defsError ? (
          <div className="text-center py-8 text-semantic-error">Failed to load metric definitions</div>
        ) : definitions?.length === 0 ? (
          <div className="text-center py-8 text-text-tertiary">
            No metric definitions yet. Click "Add Definition" to create one.
          </div>
        ) : (
          <div className="overflow-x-auto max-h-64 overflow-y-auto">
            <table className="w-full">
              <thead className="sticky top-0 z-10">
                <tr className="border-b border-glass-border">
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary w-32">Code</th>
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary w-48">Name</th>
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary w-56">Dimension</th>
                  <th className="text-right py-3 px-4 text-sm font-medium text-text-secondary w-48">Latest</th>
                  <th className="text-center py-3 px-4 text-sm font-medium text-text-secondary hidden md:table-cell w-24">Status</th>
                  <th className="text-right py-3 px-4 text-sm font-medium text-text-secondary w-24">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-glass-border">
                {definitions?.map((def: any) => {
                  const linkedDimension = def.dimensionId ? dimensionLookup.get(def.dimensionId) : null;
                  return (
                  <tr
                    key={def.id}
                    className="hover:bg-background-hover/50 cursor-pointer"
                    onClick={() => navigate(`/metrics/${def.code}/records`)}
                  >
                    <td className="py-2 px-4">
                      <span className="font-mono text-sm text-accent-purple">{def.code}</span>
                    </td>
                    <td className="py-2 px-4 text-sm text-text-primary">{def.name}</td>
                    <td className="py-2 px-4">
                      {linkedDimension ? (
                        <span className="inline-flex items-center gap-1.5 px-2 py-1 rounded-full bg-accent-cyan/10 border border-accent-cyan/20">
                          <Link2 className="w-3 h-3 text-accent-cyan" />
                          <span className="text-xs font-medium text-accent-cyan">{linkedDimension?.name}</span>
                        </span>
                      ) : def.dimensionCode ? (
                        <span className="inline-flex items-center gap-1.5 px-2 py-1 rounded-full bg-accent-cyan/10 border border-accent-cyan/20">
                          <Link2 className="w-3 h-3 text-accent-cyan" />
                          <span className="text-xs font-medium text-accent-cyan">{def.dimensionCode}</span>
                        </span>
                      ) : (
                        <span className="text-xs text-text-tertiary">—</span>
                      )}
                    </td>
                    <td className="py-2 px-4 text-right">
                      {def.latestValue !== undefined && def.latestValue !== null ? (
                        <div className="flex flex-col items-end">
                          <span className="text-sm font-medium text-accent-green">{def.latestValue} {def.unit}</span>
                          {def.latestRecordedAt && (
                            <span className="text-xs text-text-tertiary">
                              {new Date(def.latestRecordedAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
                              {' '}
                              {new Date(def.latestRecordedAt).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
                            </span>
                          )}
                        </div>
                      ) : (
                        <span className="text-text-tertiary text-sm">—</span>
                      )}
                    </td>
                    <td className="py-2 px-4 text-center hidden md:table-cell">
                      {(() => {
                        // Check if metric is outdated (no value or older than 25 hours)
                        const isOutdated = !def.latestValue || 
                          (def.latestRecordedAt && 
                           (Date.now() - new Date(def.latestRecordedAt).getTime()) > 25 * 60 * 60 * 1000);
                        
                        if (!def.isActive) {
                          return (
                            <span className="inline-flex items-center gap-1 text-xs text-text-tertiary">
                              <XCircle className="w-3.5 h-3.5" />
                              Inactive
                            </span>
                          );
                        }
                        
                        if (isOutdated) {
                          return (
                            <span className="inline-flex items-center gap-1 text-xs text-semantic-warning">
                              <AlertCircle className="w-3.5 h-3.5" />
                              Outdated
                            </span>
                          );
                        }
                        
                        return (
                          <span className="inline-flex items-center gap-1 text-xs text-semantic-success">
                            <CheckCircle className="w-3.5 h-3.5" />
                            Active
                          </span>
                        );
                      })()}
                    </td>
                    <td className="py-2 px-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            openEditModal(def);
                          }}
                          className="p-1.5 rounded-lg hover:bg-background-hover transition-colors text-text-secondary hover:text-accent-purple"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDelete(def.code);
                          }}
                          className="p-1.5 rounded-lg hover:bg-background-hover transition-colors text-text-secondary hover:text-semantic-error"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                );})}
              </tbody>
            </table>
          </div>
        )}
      </GlassCard>

      {/* Playground - Bottom Half */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 flex-1 min-h-0">
        {/* Editor Panel */}
        <GlassCard variant="default" className="p-0 overflow-hidden flex flex-col min-h-0">
          <div className="p-4 border-b border-glass-border flex items-center justify-between">
            <div className="flex items-center gap-3">
              <FileJson className="w-5 h-5 text-accent-purple" />
              <h2 className="text-lg font-semibold text-text-primary">API Playground</h2>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setShowSchema(!showSchema)}
                className="px-3 py-1.5 text-sm text-text-secondary hover:text-text-primary transition-colors"
              >
                {showSchema ? 'Hide' : 'Show'} Schema
              </button>
              <button
                onClick={handleCopy}
                className="p-2 text-text-secondary hover:text-text-primary transition-colors"
                title="Copy to clipboard"
              >
                <Copy className="w-4 h-4" />
              </button>
              <button
                onClick={handleReset}
                className="p-2 text-text-secondary hover:text-text-primary transition-colors"
                title="Reset to example"
              >
                <RefreshCw className="w-4 h-4" />
              </button>
            </div>
          </div>

          {showSchema && (
            <div className="p-4 bg-bg-tertiary border-b border-glass-border">
              <h3 className="text-sm font-medium text-text-primary mb-2">JSON Schema</h3>
              <pre className="text-xs text-text-secondary overflow-x-auto">
                {JSON.stringify(jsonSchema, null, 2)}
              </pre>
            </div>
          )}

          <div className="flex-1 min-h-0">
            <Editor
              height="100%"
              defaultLanguage="json"
              value={code}
              onChange={(value) => setCode(value || '')}
              theme="vs-dark"
              onMount={(_editor, monaco) => {
                console.log('[Monaco] onMount called, definitions count:', definitions?.length || 0);
                // Store Monaco reference for later schema updates
                monacoRef.current = monaco;
                console.log('[Monaco] Monaco ref stored:', !!monacoRef.current);
                
                // Initial schema setup (will be updated by useEffect when definitions load)
                const metricProperties: Record<string, any> = {};
                
                if (definitions && definitions.length > 0) {
                  definitions.forEach((def: any) => {
                    metricProperties[def.code] = {
                      type: 'number',
                      description: `${def.name} (${def.unit || 'no unit'})`
                    };
                  });
                }

                const schema = {
                  type: 'object',
                  properties: {
                    source: {
                      type: 'string',
                      description: 'Source of the metrics (e.g., playground, n8n, ios_shortcuts)',
                      default: 'playground'
                    },
                    metrics: {
                      type: 'object',
                      description: 'Metric values to record',
                      properties: metricProperties,
                      // Only enforce if we have definitions
                      additionalProperties: Object.keys(metricProperties).length === 0
                    }
                  },
                  required: ['metrics']
                };

                monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
                  validate: true,
                  schemas: [{
                    uri: 'http://lifeos/metrics-schema.json',
                    fileMatch: ['*'],
                    schema: schema
                  }]
                });
              }}
              options={{
                minimap: { enabled: false },
                fontSize: 14,
                lineNumbers: 'on',
                scrollBeyondLastLine: false,
                automaticLayout: true,
                tabSize: 2,
                formatOnPaste: true,
                formatOnType: true,
                wordWrap: 'on',
                wrappingIndent: 'indent',
                scrollbar: {
                  horizontal: 'hidden',
                  verticalScrollbarSize: 10,
                },
                quickSuggestions: {
                  other: true,
                  comments: false,
                  strings: true
                },
                suggest: {
                  showWords: false,
                  showProperties: true
                }
              }}
            />
          </div>

          <div className="p-4 border-t border-glass-border space-y-4">
            {/* Validation Status */}
            <div className={cn(
              "flex items-center gap-2 text-sm",
              isValid ? "text-green-400" : "text-red-400"
            )}>
              {isValid ? (
                <>
                  <Check className="w-4 h-4" />
                  <span>Valid JSON - Ready to send</span>
                </>
              ) : (
                <>
                  <AlertCircle className="w-4 h-4" />
                  <span>{validationError}</span>
                </>
              )}
            </div>

            {/* Send Button */}
            <button
              onClick={handleSend}
              disabled={!isValid || isLoading}
              className={cn(
                "w-full py-3 rounded-lg font-medium transition-all flex items-center justify-center gap-2",
                isValid && !isLoading
                  ? "bg-accent-purple hover:bg-accent-purple/80 text-white cursor-pointer"
                  : "bg-bg-tertiary text-text-tertiary cursor-not-allowed"
              )}
            >
              {isLoading ? (
                <>
                  <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  Sending...
                </>
              ) : (
                <>
                  <Play className="w-4 h-4" />
                  Send to API
                </>
              )}
            </button>
          </div>
        </GlassCard>

        {/* Event Log Panel */}
        <GlassCard variant="default" className="p-0 overflow-hidden flex flex-col min-h-0">
          <div className="p-4 border-b border-glass-border flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Clock className="w-5 h-5 text-accent-cyan" />
              <h2 className="text-lg font-semibold text-text-primary">Event Log</h2>
            </div>
            <button
              onClick={loadEventLog}
              disabled={loadingEvents}
              className="p-2 text-text-secondary hover:text-text-primary transition-colors"
            >
              <RefreshCw className={cn("w-4 h-4", loadingEvents && "animate-spin")} />
            </button>
          </div>

          <div className="flex-1 overflow-y-auto min-h-0">
            {eventLog.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full text-text-tertiary">
                <Clock className="w-12 h-12 mb-3 opacity-50" />
                <p>No events yet</p>
                <p className="text-sm">Send a request to see it here</p>
              </div>
            ) : (
              <div className="divide-y divide-glass-border">
                {eventLog.map((event: any) => {
                  // Get metric definition details
                  const def = definitions?.find((d: any) => d.code === event.metricCode);
                  const metricName = def?.name || event.metricCode;
                  const metricValue = event.value != null ? `${event.value}${def?.unit ? ` ${def.unit}` : ''}` : 'N/A';
                  
                  return (
                  <div key={event.id} className="px-3 py-2 hover:bg-bg-tertiary/50 transition-colors">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2 flex-1 min-w-0">
                        <span className={cn(
                          "w-1.5 h-1.5 rounded-full flex-shrink-0",
                          event.status === 'success' ? "bg-green-400" :
                          event.status === 'error' ? "bg-red-400" :
                          "bg-yellow-400"
                        )} />
                        <span className="text-xs text-text-secondary truncate">
                          <span className="text-text-tertiary">{event.source}</span>
                          {' › '}
                          <span className="text-accent-purple font-medium">{metricName}</span>
                          {' › '}
                          <span className="text-accent-cyan font-semibold">{metricValue}</span>
                        </span>
                      </div>
                      <span className="text-xs text-text-tertiary ml-2 flex-shrink-0">
                        {formatTimestamp(event.timestamp)}
                      </span>
                    </div>
                  </div>
                  );
                })}
              </div>
            )}
          </div>
        </GlassCard>
      </div>
      </div>

      {/* Definition Modal */}
      <DefinitionModal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEditingDefinition(null);
        }}
        definition={editingDefinition}
        onSubmit={handleCreateOrUpdate}
        isLoading={isCreating || isUpdatingDef}
        dimensions={dimensions}
      />
    </div>
  );
}
