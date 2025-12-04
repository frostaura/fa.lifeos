import { useState, useEffect, useRef } from 'react';
import Editor from '@monaco-editor/react';
import { Play, Check, AlertCircle, Clock, RefreshCw, Copy, FileJson, Plus, Edit2, Trash2, ChevronLeft, Activity, CheckCircle, XCircle, Link2 } from 'lucide-react';
import { cn } from '@utils/cn';
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
  useGetMetricRecordsQuery,
  useUpdateMetricRecordMutation,
  useDeleteMetricRecordMutation,
  useGetDimensionsQuery,
  type MetricDefinition,
  type MetricRecord,
  type CreateMetricDefinitionRequest,
  type UpdateMetricDefinitionRequest,
  type UpdateMetricRecordRequest,
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

interface CollectResponse {
  data: {
    type: string;
    attributes: {
      recorded: number;
      failed: number;
      timestamp: string;
      source: string;
    };
    records: Array<{
      code: string;
      status: string;
      id?: string;
      error?: string;
    }>;
  };
}

const examplePayload = {
  source: "playground",
  timestamp: new Date().toISOString(),
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
    timestamp: {
      type: "string",
      format: "date-time",
      description: "ISO 8601 timestamp. Defaults to current time if not provided."
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
  const dimensionOptions = dimensions.map((d) => ({
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
interface RecordModalProps {
  isOpen: boolean;
  onClose: () => void;
  record: MetricRecord | null;
  onSubmit: (data: UpdateMetricRecordRequest & { id: string }) => Promise<void>;
  isLoading: boolean;
}

function RecordModal({ isOpen, onClose, record, onSubmit, isLoading }: RecordModalProps) {
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  const [value, setValue] = useState('');
  const [notes, setNotes] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (record) {
      setValue(String(record.value));
      setNotes(record.notes || '');
    }
    setErrors({});
  }, [record, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};

    if (!value.trim()) newErrors.value = 'Value is required';
    if (isNaN(parseFloat(value))) newErrors.value = 'Value must be a number';

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    await onSubmit({
      id: record!.id,
      valueNumber: parseFloat(value),
      notes: notes || undefined,
    });
    onClose();
  };

  if (!isOpen || !record) return null;

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
          <h2 className="text-xl font-semibold text-text-primary">Edit Metric Record</h2>
          <button onClick={onClose} className="p-2 rounded-lg hover:bg-background-hover transition-colors">
            <XCircle className="w-5 h-5 text-text-tertiary" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="p-3 bg-background-hover/50 rounded-lg">
            <p className="text-sm text-text-secondary">Recorded: {new Date(record.recordedAt).toLocaleString()}</p>
            <p className="text-sm text-text-secondary">Source: {record.source || 'manual'}</p>
          </div>

          <Input
            label="Value"
            type="number"
            step="any"
            placeholder="Enter value"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            error={errors.value}
          />

          <Input
            label="Notes (Optional)"
            placeholder="Add notes..."
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={isLoading}>
              Update
            </Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}

// Records panel component
interface RecordsPanelProps {
  definition: MetricDefinition;
  onClose: () => void;
}

function RecordsPanel({ definition, onClose }: RecordsPanelProps) {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const [editingRecord, setEditingRecord] = useState<MetricRecord | null>(null);

  const { data: recordsData, isLoading, error, refetch } = useGetMetricRecordsQuery({
    code: definition.code,
    page,
    pageSize,
  });

  const [updateRecord, { isLoading: isUpdating }] = useUpdateMetricRecordMutation();
  const [deleteRecord] = useDeleteMetricRecordMutation();

  const handleUpdateRecord = async (data: UpdateMetricRecordRequest & { id: string }) => {
    await updateRecord(data).unwrap();
    refetch();
  };

  const handleDeleteRecord = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this record?')) {
      await deleteRecord(id).unwrap();
      refetch();
    }
  };

  const totalPages = recordsData?.meta?.totalPages || 1;

  return (
    <GlassCard variant="default" className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors text-text-secondary"
          >
            <ChevronLeft className="w-5 h-5" />
          </button>
          <div>
            <h2 className="text-lg font-semibold text-text-primary">{definition.name}</h2>
            <p className="text-sm text-text-tertiary">{definition.code} • {definition.unit}</p>
          </div>
        </div>
        <span className="text-sm text-text-tertiary">{recordsData?.meta?.total || 0} records</span>
      </div>

      {/* Records Table */}
      {isLoading ? (
        <div className="flex items-center justify-center h-32">
          <Spinner size="lg" />
        </div>
      ) : error ? (
        <div className="text-center py-8 text-semantic-error">Failed to load records</div>
      ) : recordsData?.data.length === 0 ? (
        <div className="text-center py-8 text-text-tertiary">
          No records yet. Use the playground below to record values.
        </div>
      ) : (
        <>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-glass-border">
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary">Recorded At</th>
                  <th className="text-right py-3 px-4 text-sm font-medium text-text-secondary">Value</th>
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary">Source</th>
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary">Notes</th>
                  <th className="text-right py-3 px-4 text-sm font-medium text-text-secondary">Actions</th>
                </tr>
              </thead>
              <tbody>
                {recordsData?.data.map((record) => (
                  <tr key={record.id} className="border-b border-glass-border/50 hover:bg-background-hover/50">
                    <td className="py-3 px-4 text-sm text-text-primary">
                      {new Date(record.recordedAt).toLocaleString()}
                    </td>
                    <td className="py-3 px-4 text-sm text-text-primary text-right font-mono">
                      {record.value} {definition.unit}
                    </td>
                    <td className="py-3 px-4 text-sm text-text-tertiary">{record.source || '-'}</td>
                    <td className="py-3 px-4 text-sm text-text-tertiary max-w-xs truncate">
                      {record.notes || '-'}
                    </td>
                    <td className="py-3 px-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => setEditingRecord(record)}
                          className="p-1.5 rounded-lg hover:bg-background-hover transition-colors text-text-secondary hover:text-accent-purple"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleDeleteRecord(record.id)}
                          className="p-1.5 rounded-lg hover:bg-background-hover transition-colors text-text-secondary hover:text-semantic-error"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-2 mt-4">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
              >
                Previous
              </Button>
              <span className="text-sm text-text-secondary">
                Page {page} of {totalPages}
              </span>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
              >
                Next
              </Button>
            </div>
          )}
        </>
      )}

      {/* Record Edit Modal */}
      <RecordModal
        isOpen={!!editingRecord}
        onClose={() => setEditingRecord(null)}
        record={editingRecord}
        onSubmit={handleUpdateRecord}
        isLoading={isUpdating}
      />
    </GlassCard>
  );
}

// Main Metrics component with both management and playground
export function Metrics() {
  const token = localStorage.getItem('accessToken');
  
  // Management state
  const { data: definitions, isLoading: isLoadingDefs, error: defsError, refetch: refetchDefs } = useGetMetricDefinitionsQuery();
  const { data: dimensionsData } = useGetDimensionsQuery();
  const [createDefinition, { isLoading: isCreating }] = useCreateMetricDefinitionMutation();
  const [updateDefinition, { isLoading: isUpdatingDef }] = useUpdateMetricDefinitionMutation();
  const [deleteDefinition] = useDeleteMetricDefinitionMutation();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingDefinition, setEditingDefinition] = useState<MetricDefinition | null>(null);
  const [selectedDefinition, setSelectedDefinition] = useState<MetricDefinition | null>(null);

  // Transform dimensions for modal and lookup
  const dimensions = (dimensionsData?.data || []).map((d) => ({
    id: d.id,
    code: d.attributes.code,
    name: d.attributes.name,
  }));
  
  // Create dimension lookup map for quick access in table
  const dimensionLookup = new Map(dimensions.map((d) => [d.id, d]));
  
  // Playground state
  const [code, setCode] = useState(JSON.stringify(examplePayload, null, 2));
  const [isValid, setIsValid] = useState(true);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<CollectResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [eventLog, setEventLog] = useState<EventLogItem[]>([]);
  const [loadingEvents, setLoadingEvents] = useState(false);
  const [showSchema, setShowSchema] = useState(false);

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
    if ('isActive' in data) {
      await updateDefinition(data as UpdateMetricDefinitionRequest & { code: string }).unwrap();
    } else {
      await createDefinition(data as CreateMetricDefinitionRequest).unwrap();
    }
    refetchDefs();
  };

  const handleDelete = async (defCode: string) => {
    if (window.confirm('Are you sure you want to delete this metric definition?')) {
      await deleteDefinition(defCode).unwrap();
      refetchDefs();
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
      const response = await fetch('/api/collect/events?limit=20', { headers });
      if (response.ok) {
        const data = await response.json();
        setEventLog(data.events || []);
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
    }
  }, [token]);

  const handleSend = async () => {
    if (!isValid) return;
    
    setIsLoading(true);
    setError(null);
    setResult(null);
    
    try {
      const payload = JSON.parse(code);
      const response = await fetch('/api/metrics/record', {
        method: 'POST',
        headers,
        body: JSON.stringify(payload),
      });
      
      const data = await response.json();
      
      if (response.ok) {
        setResult(data);
        refetchDefs(); // Refresh definitions to update record counts
        // Refresh event log after successful metric recording
        setTimeout(() => loadEventLog(), 500);
      } else {
        setError(data.message || data.error?.message || 'Failed to send metrics');
        // Also refresh event log on error to show the error event
        setTimeout(() => loadEventLog(), 500);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to send metrics');
    } finally {
      setIsLoading(false);
    }
  };

  const handleReset = () => {
    setCode(JSON.stringify({
      ...examplePayload,
      timestamp: new Date().toISOString()
    }, null, 2));
    setResult(null);
    setError(null);
  };

  const handleCopy = () => {
    navigator.clipboard.writeText(code);
  };

  const formatTimestamp = (ts: string) => {
    return new Date(ts).toLocaleString();
  };

  // If a definition is selected, show its records
  if (selectedDefinition) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary">Metrics</h1>
          <p className="text-text-secondary mt-1">View and manage metric records</p>
        </div>
        <RecordsPanel definition={selectedDefinition} onClose={() => setSelectedDefinition(null)} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-text-primary">Metrics</h1>
          <p className="text-text-secondary mt-1">Manage metric definitions and test the collection API</p>
        </div>
        <Button onClick={openCreateModal} icon={<Plus className="w-4 h-4" />}>
          Add Definition
        </Button>
      </div>

      {/* Quick Add section removed - all metrics come from definitions */}

      {/* Definitions Table - Top Half */}
      <GlassCard variant="default" className="p-6">
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
                <tr className="border-b border-glass-border bg-[#1a1a2e]">
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary bg-[#1a1a2e]">Code</th>
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary bg-[#1a1a2e]">Name</th>
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary bg-[#1a1a2e]">Dimension</th>
                  <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary bg-[#1a1a2e]">Unit</th>
                  <th className="text-right py-3 px-4 text-sm font-medium text-text-secondary bg-[#1a1a2e]">Latest</th>
                  <th className="text-center py-3 px-4 text-sm font-medium text-text-secondary bg-[#1a1a2e] hidden md:table-cell">Status</th>
                  <th className="text-right py-3 px-4 text-sm font-medium text-text-secondary bg-[#1a1a2e]">Actions</th>
                </tr>
              </thead>
              <tbody>
                {definitions?.map((def) => {
                  const linkedDimension = def.dimensionId ? dimensionLookup.get(def.dimensionId) : null;
                  return (
                  <tr
                    key={def.id}
                    className="border-b border-glass-border/50 hover:bg-background-hover/50 cursor-pointer"
                    onClick={() => setSelectedDefinition(def)}
                  >
                    <td className="py-3 px-4">
                      <span className="font-mono text-sm text-accent-purple">{def.code}</span>
                    </td>
                    <td className="py-3 px-4 text-sm text-text-primary">{def.name}</td>
                    <td className="py-3 px-4">
                      {linkedDimension ? (
                        <span className="inline-flex items-center gap-1.5 px-2 py-1 rounded-full bg-accent-cyan/10 border border-accent-cyan/20">
                          <Link2 className="w-3 h-3 text-accent-cyan" />
                          <span className="text-xs font-medium text-accent-cyan">{linkedDimension.name}</span>
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
                    <td className="py-3 px-4 text-sm text-text-tertiary">{def.unit}</td>
                    <td className="py-3 px-4 text-right">
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
                    <td className="py-3 px-4 text-center hidden md:table-cell">
                      {def.isActive ? (
                        <span className="inline-flex items-center gap-1 text-xs text-semantic-success">
                          <CheckCircle className="w-3.5 h-3.5" />
                          Active
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-xs text-text-tertiary">
                          <XCircle className="w-3.5 h-3.5" />
                          Inactive
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-right">
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
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Editor Panel */}
        <div className="bg-bg-secondary rounded-xl border border-border-primary overflow-hidden">
          <div className="p-4 border-b border-border-primary flex items-center justify-between">
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
            <div className="p-4 bg-bg-tertiary border-b border-border-primary">
              <h3 className="text-sm font-medium text-text-primary mb-2">JSON Schema</h3>
              <pre className="text-xs text-text-secondary overflow-x-auto">
                {JSON.stringify(jsonSchema, null, 2)}
              </pre>
            </div>
          )}

          <div className="h-[400px]">
            <Editor
              height="100%"
              defaultLanguage="json"
              value={code}
              onChange={(value) => setCode(value || '')}
              theme="vs-dark"
              options={{
                minimap: { enabled: false },
                fontSize: 14,
                lineNumbers: 'on',
                scrollBeyondLastLine: false,
                automaticLayout: true,
                tabSize: 2,
                formatOnPaste: true,
                formatOnType: true,
              }}
            />
          </div>

          <div className="p-4 border-t border-border-primary space-y-4">
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

            {/* Result */}
            {result && (
              <div className="p-4 bg-green-500/10 border border-green-500/30 rounded-lg">
                <div className="flex items-center gap-2 text-green-400 font-medium mb-2">
                  <Check className="w-4 h-4" />
                  Success!
                </div>
                <div className="text-sm text-text-secondary space-y-1">
                  <p>Recorded: <span className="text-text-primary">{result.data.attributes.recorded} metrics</span></p>
                  {result.data.attributes.failed > 0 && (
                    <p>Failed: <span className="text-semantic-error">{result.data.attributes.failed}</span></p>
                  )}
                  <p>Source: <span className="text-text-primary">{result.data.attributes.source}</span></p>
                  <p>Timestamp: <span className="text-text-primary">{formatTimestamp(result.data.attributes.timestamp)}</span></p>
                </div>
              </div>
            )}

            {/* Error */}
            {error && (
              <div className="p-4 bg-red-500/10 border border-red-500/30 rounded-lg">
                <div className="flex items-center gap-2 text-red-400 font-medium mb-2">
                  <AlertCircle className="w-4 h-4" />
                  Error
                </div>
                <p className="text-sm text-text-secondary">{error}</p>
              </div>
            )}
          </div>
        </div>

        {/* Event Log Panel */}
        <div className="bg-bg-secondary rounded-xl border border-border-primary overflow-hidden">
          <div className="p-4 border-b border-border-primary flex items-center justify-between">
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

          <div className="h-[560px] overflow-y-auto">
            {eventLog.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full text-text-tertiary">
                <Clock className="w-12 h-12 mb-3 opacity-50" />
                <p>No events yet</p>
                <p className="text-sm">Send a request to see it here</p>
              </div>
            ) : (
              <div className="divide-y divide-border-primary">
                {eventLog.map((event) => (
                  <div key={event.id} className="p-4 hover:bg-bg-tertiary/50 transition-colors">
                    <div className="flex items-start justify-between mb-2">
                      <div className="flex items-center gap-2">
                        <span className={cn(
                          "w-2 h-2 rounded-full",
                          event.status === 'success' ? "bg-green-400" :
                          event.status === 'error' ? "bg-red-400" :
                          "bg-yellow-400"
                        )} />
                        <span className="font-medium text-text-primary">{event.eventType}</span>
                      </div>
                      <span className="text-xs text-text-tertiary">
                        {formatTimestamp(event.timestamp)}
                      </span>
                    </div>
                    <div className="text-sm text-text-secondary space-y-1">
                      <p>Source: <span className="text-text-primary">{event.source}</span></p>
                      <p>Status: <span className={cn(
                        event.status === 'success' ? "text-green-400" :
                        event.status === 'error' ? "text-red-400" :
                        "text-yellow-400"
                      )}>{event.status}</span></p>
                      {event.errorMessage && (
                        <p className="text-red-400 text-xs mt-2">{event.errorMessage}</p>
                      )}
                    </div>
                    {event.requestPayload && (
                      <details className="mt-2">
                        <summary className="text-xs text-text-tertiary cursor-pointer hover:text-text-secondary">
                          View Payload
                        </summary>
                        <pre className="mt-2 p-2 bg-bg-tertiary rounded text-xs text-text-secondary overflow-x-auto">
                          {JSON.stringify(JSON.parse(event.requestPayload), null, 2)}
                        </pre>
                      </details>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
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
