'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';

interface Dish {
  id: string;
  name: string;
  activeMinutes: number;
  totalMinutes: number;
}

interface WeeklyPlanItem {
  dayIndex: number;
  dish: Dish;
}

interface WeeklyPlan {
  weekStartDate: string;
  items: WeeklyPlanItem[];
}

interface PlannedDishExplanation {
  dishId: string;
  reasons: string[];
}

interface GenerateWeeklyPlanResponse {
  plan: WeeklyPlan;
  explanationsByDay: { [key: number]: PlannedDishExplanation };
}

interface DishListResponse {
  items: Dish[];
}

interface RuleViolation {
  ruleCode: string;
  message: string;
  dayIndices: number[];
}

interface UpdateWeeklyPlanResponse {
  weekStartDate: string;
  status: string;
  violations: RuleViolation[];
}

const dayNames = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

export default function WeeklyPlanPage() {
  const [weekStart, setWeekStart] = useState('');
  const [plan, setPlan] = useState<WeeklyPlan | null>(null);
  const [explanations, setExplanations] = useState<{ [key: number]: PlannedDishExplanation } | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);
  const [dishes, setDishes] = useState<Dish[]>([]);
  const [editMode, setEditMode] = useState(false);
  const [editedPlan, setEditedPlan] = useState<{ [dayIndex: number]: string }>({});
  const [saving, setSaving] = useState(false);
  const [violations, setViolations] = useState<RuleViolation[]>([]);

  const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';

  useEffect(() => {
    fetchDishes();
    const monday = getMonday(new Date());
    setWeekStart(monday);
    loadPlanForWeek(monday);
  }, []);

  const fetchDishes = async () => {
    try {
      const response = await fetch(`${apiUrl}/dishes`);
      if (!response.ok) {
        throw new Error(`Failed to fetch dishes: ${response.status}`);
      }
      const data: DishListResponse = await response.json();
      setDishes(data.items);
    } catch (err) {
      console.error('Failed to load dishes:', err);
    }
  };

  const getMonday = (date: Date): string => {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    d.setDate(diff);
    return d.toISOString().split('T')[0];
  };

  const loadPlanForWeek = async (dateStr: string) => {
    setLoading(true);
    setError(null);
    setPlan(null);
    setViolations([]);
    setEditMode(false);

    try {
      const response = await fetch(`${apiUrl}/weekly-plan/${encodeURIComponent(dateStr)}`);

      if (response.status === 404) {
        setError('No plan found for this week. Generate one or create a new plan.');
        setEditMode(true);
        initializeEmptyPlan();
        return;
      }

      if (!response.ok) {
        throw new Error(`Failed to load plan: ${response.status}`);
      }

      const data: WeeklyPlan = await response.json();
      setPlan(data);
      setExplanations(null);
      initializeEditedPlanFromExisting(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load plan');
    } finally {
      setLoading(false);
    }
  };

  const initializeEmptyPlan = () => {
    const emptyPlan: { [dayIndex: number]: string } = {};
    for (let i = 0; i < 7; i++) {
      emptyPlan[i] = '';
    }
    setEditedPlan(emptyPlan);
  };

  const initializeEditedPlanFromExisting = (existingPlan: WeeklyPlan) => {
    const editPlan: { [dayIndex: number]: string } = {};
    existingPlan.items.forEach(item => {
      editPlan[item.dayIndex] = item.dish.id;
    });
    setEditedPlan(editPlan);
  };

  const handleWeekChange = (newWeekStart: string) => {
    setWeekStart(newWeekStart);
    loadPlanForWeek(newWeekStart);
  };

  const handleGeneratePlan = async () => {
    const dateStr = weekStart || getMonday(new Date());
    
    setGenerating(true);
    setError(null);
    setViolations([]);

    try {
      const response = await fetch(`${apiUrl}/weekly-plan/generate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          weekStartDate: dateStr,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || `Failed to generate plan: ${response.status}`);
      }

      const result: GenerateWeeklyPlanResponse = await response.json();
      setPlan(result.plan);
      setExplanations(result.explanationsByDay);
      setWeekStart(dateStr);
      setEditMode(false);
      initializeEditedPlanFromExisting(result.plan);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate plan');
    } finally {
      setGenerating(false);
    }
  };

  const handleEditPlan = () => {
    setEditMode(true);
    setExplanations(null);
  };

  const handleCancelEdit = () => {
    setEditMode(false);
    setViolations([]);
    if (plan) {
      initializeEditedPlanFromExisting(plan);
    }
  };

  const handleDishChange = (dayIndex: number, dishId: string) => {
    setEditedPlan(prev => ({
      ...prev,
      [dayIndex]: dishId
    }));
  };

  const handleSavePlan = async () => {
    const dateStr = weekStart || getMonday(new Date());
    
    // Validate that all days have a dish selected
    const allDaysSelected = Object.keys(editedPlan).length === 7 && 
      Object.values(editedPlan).every(dishId => dishId !== '');
    
    if (!allDaysSelected) {
      setError('Please select a dish for all 7 days before saving.');
      return;
    }

    setSaving(true);
    setError(null);
    setViolations([]);

    try {
      const items = Object.entries(editedPlan).map(([dayIndex, dishId]) => ({
        dayIndex: parseInt(dayIndex),
        dishId: dishId
      }));

      const response = await fetch(`${apiUrl}/weekly-plan/${encodeURIComponent(dateStr)}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          weekStartDate: dateStr,
          items: items
        }),
      });

      const result: UpdateWeeklyPlanResponse = await response.json();

      if (!response.ok || result.status === 'validation_failed') {
        if (result.violations && result.violations.length > 0) {
          setViolations(result.violations);
          setError('Plan has validation errors. Please fix them before saving.');
        } else {
          throw new Error(`Failed to save plan: ${response.status}`);
        }
        return;
      }

      // Success - reload the plan
      setEditMode(false);
      await loadPlanForWeek(dateStr);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save plan');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
      <div style={{ marginBottom: '2rem' }}>
        <Link href="/" style={{ color: 'blue', textDecoration: 'underline' }}>
          ← Back to Home
        </Link>
      </div>

      <h1>Weekly Plan</h1>

      <div style={{ margin: '2rem 0', padding: '1rem', border: '1px solid #ccc', borderRadius: '4px' }}>
        <h2>Select Week</h2>
        <div style={{ display: 'flex', gap: '1rem', alignItems: 'center', marginTop: '1rem' }}>
          <label>
            Week start (Monday):
            <input
              type="date"
              value={weekStart}
              onChange={(e) => handleWeekChange(e.target.value)}
              style={{ marginLeft: '0.5rem', padding: '0.5rem' }}
            />
          </label>
          <button
            onClick={() => handleWeekChange(getMonday(new Date()))}
            style={{ padding: '0.5rem 1rem', cursor: 'pointer' }}
          >
            Use This Week
          </button>
        </div>

        <div style={{ display: 'flex', gap: '1rem', marginTop: '1rem' }}>
          <button
            onClick={handleGeneratePlan}
            disabled={generating}
            style={{
              padding: '0.75rem 1.5rem',
              backgroundColor: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: generating ? 'not-allowed' : 'pointer',
              opacity: generating ? 0.6 : 1,
            }}
          >
            {generating ? 'Generating...' : 'Generate New Plan'}
          </button>

          {plan && !editMode && (
            <button
              onClick={handleEditPlan}
              style={{
                padding: '0.75rem 1.5rem',
                backgroundColor: '#FF9800',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              Edit Plan
            </button>
          )}
        </div>
      </div>

      {error && (
        <div style={{ 
          margin: '2rem 0', 
          padding: '1rem', 
          border: '1px solid red', 
          borderRadius: '4px',
          backgroundColor: '#ffebee',
          color: 'red'
        }}>
          <strong>Error:</strong> {error}
        </div>
      )}

      {violations.length > 0 && (
        <div style={{ 
          margin: '2rem 0', 
          padding: '1rem', 
          border: '1px solid orange', 
          borderRadius: '4px',
          backgroundColor: '#fff3cd',
          color: '#856404'
        }}>
          <h3 style={{ marginTop: 0 }}>Validation Errors</h3>
          {violations.map((violation, idx) => (
            <div key={idx} style={{ marginBottom: '0.5rem' }}>
              <strong>{violation.ruleCode}:</strong> {violation.message}
              {violation.dayIndices.length > 0 && (
                <span style={{ marginLeft: '0.5rem', fontSize: '0.9rem' }}>
                  (Days: {violation.dayIndices.map(d => dayNames[d]).join(', ')})
                </span>
              )}
            </div>
          ))}
        </div>
      )}

      {editMode && (
        <div style={{ margin: '2rem 0' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
            <h2>Edit Weekly Plan for {weekStart}</h2>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <button
                onClick={handleSavePlan}
                disabled={saving}
                style={{
                  padding: '0.75rem 1.5rem',
                  backgroundColor: '#2196F3',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: saving ? 'not-allowed' : 'pointer',
                  opacity: saving ? 0.6 : 1,
                }}
              >
                {saving ? 'Saving...' : 'Save Plan'}
              </button>
              <button
                onClick={handleCancelEdit}
                disabled={saving}
                style={{
                  padding: '0.75rem 1.5rem',
                  backgroundColor: '#6c757d',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: saving ? 'not-allowed' : 'pointer',
                  opacity: saving ? 0.6 : 1,
                }}
              >
                Cancel
              </button>
            </div>
          </div>

          <div style={{ display: 'grid', gap: '1rem' }}>
            {[0, 1, 2, 3, 4, 5, 6].map((dayIndex) => {
              const selectedDish = dishes.find(d => d.id === editedPlan[dayIndex]);
              return (
                <div
                  key={dayIndex}
                  style={{
                    border: '1px solid #ccc',
                    borderRadius: '4px',
                    padding: '1rem',
                    backgroundColor: '#f9f9f9',
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                    <label style={{ minWidth: '100px', fontWeight: 'bold' }}>
                      {dayNames[dayIndex]}:
                    </label>
                    <select
                      value={editedPlan[dayIndex] || ''}
                      onChange={(e) => handleDishChange(dayIndex, e.target.value)}
                      style={{
                        flex: 1,
                        padding: '0.5rem',
                        border: '1px solid #ccc',
                        borderRadius: '4px',
                        fontSize: '1rem'
                      }}
                    >
                      <option value="">-- Select a dish --</option>
                      {dishes.map(dish => (
                        <option key={dish.id} value={dish.id}>
                          {dish.name} ({dish.totalMinutes}min)
                        </option>
                      ))}
                    </select>
                    {selectedDish && (
                      <div style={{ fontSize: '0.85rem', color: '#666', minWidth: '150px' }}>
                        Active: {selectedDish.activeMinutes}min | Total: {selectedDish.totalMinutes}min
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {plan && !editMode && (
        <div style={{ margin: '2rem 0' }}>
          <h2>Weekly Plan for {plan.weekStartDate}</h2>

          <div style={{ display: 'grid', gap: '1rem', marginTop: '1rem' }}>
            {plan.items
              .sort((a, b) => a.dayIndex - b.dayIndex)
              .map((item) => (
                <div
                  key={item.dayIndex}
                  style={{
                    border: '1px solid #ccc',
                    borderRadius: '4px',
                    padding: '1rem',
                    backgroundColor: '#f9f9f9',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start' }}>
                    <div>
                      <h3 style={{ margin: 0 }}>
                        {dayNames[item.dayIndex]} - {item.dish.name}
                      </h3>
                      <div style={{ marginTop: '0.5rem', fontSize: '0.9rem', color: '#666' }}>
                        <span>Active: {item.dish.activeMinutes}min</span>
                        <span style={{ marginLeft: '1rem' }}>Total: {item.dish.totalMinutes}min</span>
                      </div>
                      {explanations && explanations[item.dayIndex] && (
                        <div style={{ marginTop: '0.5rem', fontSize: '0.85rem', color: '#555' }}>
                          <strong>Why this dish:</strong>
                          <ul style={{ marginTop: '0.25rem', marginBottom: 0 }}>
                            {explanations[item.dayIndex].reasons.map((reason, idx) => (
                              <li key={idx}>{reason}</li>
                            ))}
                          </ul>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ))}
          </div>
        </div>
      )}
    </div>
  );
}
