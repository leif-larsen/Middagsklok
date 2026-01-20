'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';

interface Dish {
  id: string;
  name: string;
  activeMinutes: number;
  totalMinutes: number;
}

interface DishListResponse {
  items: Dish[];
}

interface ImportResult {
  total: number;
  created: number;
  skipped: number;
  failed: number;
  results: {
    name: string;
    status: string;
    dishId: string | null;
    error: string | null;
  }[];
}

export default function DishesPage() {
  const [dishes, setDishes] = useState<Dish[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [importing, setImporting] = useState(false);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  
  // Filter state
  const [searchQuery, setSearchQuery] = useState('');
  const [maxTotalMinutes, setMaxTotalMinutes] = useState<number | ''>('');

  const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';

  useEffect(() => {
    fetchDishes();
  }, []);

  const fetchDishes = async () => {
    try {
      const response = await fetch(`${apiUrl}/dishes`);
      if (!response.ok) {
        throw new Error(`Failed to fetch dishes: ${response.status}`);
      }
      const data: DishListResponse = await response.json();
      setDishes(data.items);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load dishes');
    } finally {
      setLoading(false);
    }
  };

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setImporting(true);
    setImportResult(null);

    try {
      const text = await file.text();
      const json = JSON.parse(text);

      const response = await fetch(`${apiUrl}/dishes/import`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(json),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || `Import failed: ${response.status}`);
      }

      const result = await response.json();
      setImportResult(result);

      // Refresh dishes list
      await fetchDishes();
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to import dishes');
    } finally {
      setImporting(false);
      // Reset file input
      event.target.value = '';
    }
  };

  // Filter dishes based on search query and max total minutes
  const filteredDishes = dishes.filter(dish => {
    // Filter by name (case-insensitive)
    const matchesSearch = dish.name.toLowerCase().includes(searchQuery.toLowerCase());
    
    // Filter by max total minutes
    const matchesMaxTime = maxTotalMinutes === '' || dish.totalMinutes <= maxTotalMinutes;
    
    return matchesSearch && matchesMaxTime;
  });

  return (
    <div style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
      <div style={{ marginBottom: '2rem' }}>
        <Link href="/" style={{ color: 'blue', textDecoration: 'underline' }}>
          ← Back to Home
        </Link>
      </div>

      <h1>Dishes</h1>

      <div style={{ margin: '2rem 0', padding: '1rem', border: '1px solid #ccc', borderRadius: '4px' }}>
        <h2>Import Dishes</h2>
        <p style={{ fontSize: '0.9rem', color: '#666' }}>
          Upload a JSON file with dishes to import. Format: {'{'}dishes: [...]{'}'}
        </p>
        <input
          type="file"
          accept=".json"
          onChange={handleFileUpload}
          disabled={importing}
          style={{ marginTop: '0.5rem' }}
        />
        {importing && <p>Importing...</p>}
      </div>

      {importResult && (
        <div style={{ 
          margin: '2rem 0', 
          padding: '1rem', 
          border: '1px solid #ccc', 
          borderRadius: '4px',
          backgroundColor: '#f5f5f5'
        }}>
          <h3>Import Results</h3>
          <p>
            Total: {importResult.total} | 
            Created: {importResult.created} | 
            Skipped: {importResult.skipped} | 
            Failed: {importResult.failed}
          </p>
          <div style={{ maxHeight: '200px', overflowY: 'auto', marginTop: '1rem' }}>
            {importResult.results.map((result, index) => (
              <div 
                key={index} 
                style={{ 
                  padding: '0.5rem', 
                  borderBottom: '1px solid #ddd',
                  color: result.status === 'created' ? 'green' : result.status === 'failed' ? 'red' : 'orange'
                }}
              >
                {result.status === 'created' && '✓ '}
                {result.status === 'failed' && '✗ '}
                {result.status === 'skipped' && '→ '}
                {result.name} [{result.status}]
                {result.error && ` - ${result.error}`}
              </div>
            ))}
          </div>
        </div>
      )}

      {loading && <p>Loading dishes...</p>}
      {error && <p style={{ color: 'red' }}>{error}</p>}

      {!loading && !error && (
        <div>
          {/* Search and Filter Controls */}
          <div style={{ 
            marginBottom: '2rem', 
            padding: '1rem', 
            border: '1px solid #ccc', 
            borderRadius: '4px',
            backgroundColor: '#f9f9f9'
          }}>
            <h2 style={{ marginTop: 0 }}>Søk og filtre</h2>
            
            <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
                  Søk etter navn
                </label>
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Skriv for å søke..."
                  style={{
                    width: '100%',
                    padding: '0.5rem',
                    border: '1px solid #ccc',
                    borderRadius: '4px',
                  }}
                />
              </div>
              
              <div>
                <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
                  Maks totaltid (minutter)
                </label>
                <input
                  type="number"
                  value={maxTotalMinutes}
                  onChange={(e) => setMaxTotalMinutes(e.target.value === '' ? '' : parseInt(e.target.value))}
                  placeholder="Valgfritt"
                  min="1"
                  style={{
                    width: '100%',
                    padding: '0.5rem',
                    border: '1px solid #ccc',
                    borderRadius: '4px',
                  }}
                />
              </div>
            </div>
            
            {(searchQuery || maxTotalMinutes !== '') && (
              <button
                onClick={() => {
                  setSearchQuery('');
                  setMaxTotalMinutes('');
                }}
                style={{
                  marginTop: '1rem',
                  padding: '0.5rem 1rem',
                  backgroundColor: '#6c757d',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                }}
              >
                Nullstill filtre
              </button>
            )}
          </div>

          <h2>Viser {filteredDishes.length} av {dishes.length}</h2>
          {filteredDishes.length === 0 ? (
            <p style={{ padding: '2rem', textAlign: 'center', color: '#666', fontStyle: 'italic' }}>
              {dishes.length === 0 
                ? 'No dishes found. Import some dishes to get started.' 
                : 'Ingen retter matcher søket eller filtrene. Prøv å justere søkekriteriene.'}
            </p>
          ) : (
            <div style={{ display: 'grid', gap: '1rem' }}>
              {filteredDishes.map((dish) => (
                <div
                  key={dish.id}
                  style={{
                    border: '1px solid #ccc',
                    borderRadius: '4px',
                    padding: '1rem',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <h3>{dish.name}</h3>
                    <Link 
                      href={`/dishes/${dish.id}`}
                      style={{ 
                        color: 'blue', 
                        textDecoration: 'underline',
                        fontSize: '0.9rem'
                      }}
                    >
                      Edit
                    </Link>
                  </div>
                  <div style={{ display: 'flex', gap: '2rem', fontSize: '0.9rem', color: '#666' }}>
                    <span>Active: {dish.activeMinutes}min</span>
                    <span>Total: {dish.totalMinutes}min</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
