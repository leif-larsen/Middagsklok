'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';

interface ShoppingListItem {
  ingredientName: string;
  category: string;
  amount: number;
  unit: string;
}

interface ShoppingListResponse {
  weekStartDate: string;
  items: ShoppingListItem[];
}

const dayNames = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

export default function ShoppingListPage() {
  const [weekStart, setWeekStart] = useState('');
  const [shoppingList, setShoppingList] = useState<ShoppingListResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';

  const getMonday = (date: Date): string => {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    d.setDate(diff);
    return d.toISOString().split('T')[0];
  };

  // Load current week's shopping list on mount
  useEffect(() => {
    const currentMonday = getMonday(new Date());
    setWeekStart(currentMonday);
    loadShoppingList(currentMonday);
  }, []);

  const loadShoppingList = async (dateStr: string) => {
    setLoading(true);
    setError(null);
    setShoppingList(null);

    try {
      const response = await fetch(`${apiUrl}/shopping-list/${encodeURIComponent(dateStr)}`);

      if (response.status === 404) {
        setError('Ingen handleliste funnet for denne uken. Generer en ukesplan først.');
        return;
      }

      if (!response.ok) {
        throw new Error(`Kunne ikke laste handleliste: ${response.status}`);
      }

      const data: ShoppingListResponse = await response.json();
      setShoppingList(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Kunne ikke laste handleliste');
    } finally {
      setLoading(false);
    }
  };

  const handleLoadShoppingList = () => {
    const dateStr = weekStart || getMonday(new Date());
    loadShoppingList(dateStr);
  };

  // Group items by category
  const groupedItems = shoppingList?.items.reduce((acc, item) => {
    if (!acc[item.category]) {
      acc[item.category] = [];
    }
    acc[item.category].push(item);
    return acc;
  }, {} as Record<string, ShoppingListItem[]>) || {};

  const categories = Object.keys(groupedItems).sort();

  return (
    <div style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
      <div style={{ marginBottom: '2rem' }}>
        <Link href="/" style={{ color: 'blue', textDecoration: 'underline' }}>
          ← Back to Home
        </Link>
      </div>

      <h1>Handleliste</h1>

      <div style={{ margin: '2rem 0', padding: '1rem', border: '1px solid #ccc', borderRadius: '4px' }}>
        <h2>Velg uke</h2>
        <div style={{ display: 'flex', gap: '1rem', alignItems: 'center', marginTop: '1rem' }}>
          <label>
            Ukestart (mandag):
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
            Bruk denne uken
          </button>
        </div>

        <div style={{ marginTop: '1rem' }}>
          <button
            onClick={handleLoadShoppingList}
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
            {loading ? 'Laster...' : 'Last handleliste'}
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
          <strong>Feil:</strong> {error}
        </div>
      )}

      {shoppingList && (
        <div style={{ margin: '2rem 0' }}>
          <h2>Handleliste for uke {shoppingList.weekStartDate}</h2>

          {categories.length === 0 ? (
            <p style={{ marginTop: '1rem', color: '#666' }}>
              Ingen varer på handlelisten for denne uken.
            </p>
          ) : (
            <div style={{ marginTop: '1rem' }}>
              {categories.map((category) => (
                <div key={category} style={{ marginBottom: '2rem' }}>
                  <h3 style={{ 
                    margin: '1rem 0 0.5rem 0', 
                    borderBottom: '2px solid #ccc', 
                    paddingBottom: '0.5rem',
                    fontSize: '1.2rem'
                  }}>
                    {category}
                  </h3>
                  <ul style={{ 
                    listStyle: 'none', 
                    padding: 0, 
                    margin: 0 
                  }}>
                    {groupedItems[category].map((item, idx) => (
                      <li 
                        key={idx}
                        style={{ 
                          padding: '0.75rem 1rem',
                          backgroundColor: idx % 2 === 0 ? '#f9f9f9' : 'white',
                          border: '1px solid #e0e0e0',
                          borderTop: idx === 0 ? '1px solid #e0e0e0' : 'none',
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center'
                        }}
                      >
                        <span style={{ fontWeight: 500 }}>{item.ingredientName}</span>
                        <span style={{ color: '#666' }}>
                          {item.amount} {item.unit}
                        </span>
                      </li>
                    ))}
                  </ul>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
