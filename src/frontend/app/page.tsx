'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { PageTitle } from '@/components/ui-primitives';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { CheckCircle2, XCircle, Loader2 } from 'lucide-react';

interface HealthResponse {
  status: string;
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
    <div className="space-y-8">
      <div>
        <PageTitle>Middagsklok</PageTitle>
        <p className="mt-2 text-muted-foreground">
          Simple weekly meal planning
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>API Connection</CardTitle>
          <CardDescription>Backend service health status</CardDescription>
        </CardHeader>
        <CardContent>
          {loading && (
            <div className="flex items-center gap-2 text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span>Checking API...</span>
            </div>
          )}
          {health && (
            <Alert className="border-green-200 bg-green-50 text-green-900">
              <CheckCircle2 className="h-4 w-4 text-green-600" />
              <AlertTitle>API is healthy</AlertTitle>
              <AlertDescription className="mt-2">
                Status: <Badge variant="secondary">{health.status}</Badge>
              </AlertDescription>
            </Alert>
          )}
          {error && (
            <Alert variant="destructive">
              <XCircle className="h-4 w-4" />
              <AlertTitle>API connection failed</AlertTitle>
              <AlertDescription className="mt-2 space-y-2">
                <p>{error}</p>
                <p className="text-sm">
                  Make sure the API is running: <code className="px-1 py-0.5 bg-black/10 rounded">dotnet run --project src/Middagsklok.Api</code>
                </p>
              </AlertDescription>
            </Alert>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Quick Links</CardTitle>
          <CardDescription>Navigate to different sections</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Link 
              href="/dishes"
              className="group relative overflow-hidden rounded-lg border bg-background p-4 hover:bg-accent hover:text-accent-foreground transition-colors"
            >
              <div className="flex flex-col gap-2">
                <h3 className="font-semibold">Dishes</h3>
                <p className="text-sm text-muted-foreground">
                  View and import dishes
                </p>
              </div>
            </Link>
            <Link 
              href="/weekly-plan"
              className="group relative overflow-hidden rounded-lg border bg-background p-4 hover:bg-accent hover:text-accent-foreground transition-colors"
            >
              <div className="flex flex-col gap-2">
                <h3 className="font-semibold">Weekly Plan</h3>
                <p className="text-sm text-muted-foreground">
                  Generate and view weekly plans
                </p>
              </div>
            </Link>
            <Link 
              href="/shopping-list"
              className="group relative overflow-hidden rounded-lg border bg-background p-4 hover:bg-accent hover:text-accent-foreground transition-colors"
            >
              <div className="flex flex-col gap-2">
                <h3 className="font-semibold">Shopping List</h3>
                <p className="text-sm text-muted-foreground">
                  View shopping list for the week
                </p>
              </div>
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
