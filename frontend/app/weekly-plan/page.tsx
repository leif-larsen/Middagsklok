'use client';

import { useState } from 'react';
import Link from 'next/link';

interface Dish {
  id: string;
  name: string;
  activeMinutes: number;
  totalMinutes: number;
  kidRating: number;
  familyRating: number;
  isPescetarian: boolean;
  hasOptionalMeatVariant: boolean;
}

interface WeeklyPlanItem {
  dayIndex: number;
  dish: Dish;
}

interface WeeklyPlan {
  id: string;
  weekStartDate: string;
  createdAt: string;
  items: WeeklyPlanItem[];
}

const dayNames = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

export default function WeeklyPlanPage() {
  const [weekStart, setWeekStart] = useState('');
  const [plan, setPlan] = useState<WeeklyPlan | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);

  const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';

  const getMonday = (date: Date): string => {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    d.setDate(diff);
    return d.toISOString().split('T')[0];
  };

  const handleGeneratePlan = async () => {
    const dateStr = weekStart || getMonday(new Date());
    
    setGenerating(true);
    setError(null);

    try {
      const response = await fetch(`${apiUrl}/weekly-plans/generate`, {
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

      const result = await response.json();
      setPlan(result.plan);
      setWeekStart(dateStr);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate plan');
    } finally {
      setGenerating(false);
    }
  };

  const handleLoadPlan = async () => {
    const dateStr = weekStart || getMonday(new Date());
    
    setLoading(true);
    setError(null);
    setPlan(null);

    try {
      const response = await fetch(`${apiUrl}/weekly-plans?weekStart=${encodeURIComponent(dateStr)}`);

      if (response.status === 404) {
        setError('No plan found for this week. Generate one first.');
        return;
      }

      if (!response.ok) {
        throw new Error(`Failed to load plan: ${response.status}`);
      }

      const data = await response.json();
      setPlan(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load plan');
    } finally {
      setLoading(false);
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
              onChange={(e) => setWeekStart(e.target.value)}
              style={{ marginLeft: '0.5rem', padding: '0.5rem' }}
            />
          </label>
          <button
            onClick={() => setWeekStart(getMonday(new Date()))}
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

          <button
            onClick={handleLoadPlan}
            disabled={loading}
            style={{
              padding: '0.75rem 1.5rem',
              backgroundColor: '#2196F3',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: loading ? 'not-allowed' : 'pointer',
              opacity: loading ? 0.6 : 1,
            }}
          >
            {loading ? 'Loading...' : 'Load Existing Plan'}
          </button>
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

      {plan && (
        <div style={{ margin: '2rem 0' }}>
          <h2>Weekly Plan for {new Date(plan.weekStartDate).toLocaleDateString()}</h2>
          <p style={{ fontSize: '0.9rem', color: '#666' }}>
            Created: {new Date(plan.createdAt).toLocaleString()}
          </p>

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
                        <span style={{ marginLeft: '1rem' }}>Kid: {item.dish.kidRating}/5</span>
                        <span style={{ marginLeft: '1rem' }}>Family: {item.dish.familyRating}/5</span>
                        {item.dish.isPescetarian && (
                          <span style={{ marginLeft: '1rem', color: 'blue' }}>🐟 Pescetarian</span>
                        )}
                      </div>
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
