'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';

interface HealthResponse {
  status: string;
  timestamp: string;
}

export default function Home() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkHealth = async () => {
      try {
        const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';
        const response = await fetch(`${apiUrl}/health`);
        
        if (!response.ok) {
          throw new Error(`API returned ${response.status}`);
        }
        
        const data = await response.json();
        setHealth(data);
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to connect to API');
        setHealth(null);
      } finally {
        setLoading(false);
      }
    };

    checkHealth();
  }, []);

  return (
    <div style={{ padding: '2rem', maxWidth: '800px', margin: '0 auto' }}>
      <h1>Middagsklok</h1>
      <p>Simple weekly meal planning</p>

      <div style={{ 
        margin: '2rem 0', 
        padding: '1rem', 
        border: '1px solid #ccc', 
        borderRadius: '4px',
        backgroundColor: health ? '#e8f5e9' : error ? '#ffebee' : '#f5f5f5'
      }}>
        <h2>API Connection</h2>
        {loading && <p>Checking API...</p>}
        {health && (
          <div>
            <p style={{ color: 'green' }}>✓ API is healthy</p>
            <p style={{ fontSize: '0.9rem', color: '#666' }}>
              Status: {health.status}<br />
              Timestamp: {new Date(health.timestamp).toLocaleString()}
            </p>
          </div>
        )}
        {error && (
          <div>
            <p style={{ color: 'red' }}>✗ API connection failed</p>
            <p style={{ fontSize: '0.9rem', color: '#666' }}>{error}</p>
            <p style={{ fontSize: '0.9rem', marginTop: '1rem' }}>
              Make sure the API is running: <code>dotnet run --project src/Middagsklok.Api</code>
            </p>
          </div>
        )}
      </div>

      <div style={{ marginTop: '2rem' }}>
        <h2>Pages</h2>
        <ul style={{ lineHeight: '2' }}>
          <li>
            <Link href="/dishes" style={{ color: 'blue', textDecoration: 'underline' }}>
              Dishes
            </Link>
            {' - View and import dishes'}
          </li>
          <li>
            <Link href="/weekly-plan" style={{ color: 'blue', textDecoration: 'underline' }}>
              Weekly Plan
            </Link>
            {' - Generate and view weekly plans'}
          </li>
        </ul>
      </div>
    </div>
  );
}
