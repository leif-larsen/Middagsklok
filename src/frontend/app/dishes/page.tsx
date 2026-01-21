'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { PageTitle, EmptyState, SectionCard } from '@/components/ui-primitives';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Loader2, Upload, CheckCircle2, XCircle, AlertCircle, Search, Clock } from 'lucide-react';

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
    const matchesSearch = dish.name.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesMaxTime = maxTotalMinutes === '' || (dish.totalMinutes != null && dish.totalMinutes <= maxTotalMinutes);
    return matchesSearch && matchesMaxTime;
  });

  return (
    <div className="space-y-8">
      <PageTitle>Dishes</PageTitle>

      <Card>
        <CardHeader>
          <CardTitle>Import Dishes</CardTitle>
          <CardDescription>
            Upload a JSON file with dishes to import. Format: {'{'}dishes: [...]{'}'}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-4">
            <Input
              type="file"
              accept=".json"
              onChange={handleFileUpload}
              disabled={importing}
              className="max-w-md"
            />
            {importing && (
              <div className="flex items-center gap-2 text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                <span className="text-sm">Importing...</span>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {importResult && (
        <Alert className="border-blue-200 bg-blue-50">
          <AlertCircle className="h-4 w-4 text-blue-600" />
          <AlertTitle className="text-blue-900">Import Results</AlertTitle>
          <AlertDescription className="text-blue-900">
            <div className="mt-2 flex gap-4 text-sm">
              <span>Total: <Badge variant="secondary">{importResult.total}</Badge></span>
              <span>Created: <Badge variant="secondary">{importResult.created}</Badge></span>
              <span>Skipped: <Badge variant="secondary">{importResult.skipped}</Badge></span>
              <span>Failed: <Badge variant="secondary">{importResult.failed}</Badge></span>
            </div>
            <div className="mt-4 max-h-40 overflow-y-auto space-y-1">
              {importResult.results.map((result, index) => (
                <div key={index} className="text-sm flex items-center gap-2">
                  {result.status === 'created' && <CheckCircle2 className="h-3 w-3 text-green-600" />}
                  {result.status === 'failed' && <XCircle className="h-3 w-3 text-red-600" />}
                  {result.status === 'skipped' && <AlertCircle className="h-3 w-3 text-orange-600" />}
                  <span className={
                    result.status === 'created' ? 'text-green-900' :
                    result.status === 'failed' ? 'text-red-900' :
                    'text-orange-900'
                  }>
                    {result.name} [{result.status}]
                    {result.error && ` - ${result.error}`}
                  </span>
                </div>
              ))}
            </div>
          </AlertDescription>
        </Alert>
      )}

      {loading && (
        <div className="flex items-center justify-center py-8">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          <span className="ml-2 text-muted-foreground">Loading dishes...</span>
        </div>
      )}

      {error && (
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!loading && !error && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Search and Filter</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <label className="text-sm font-medium">Search by name</label>
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      type="text"
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      placeholder="Type to search..."
                      className="pl-9"
                    />
                  </div>
                </div>
                
                <div className="space-y-2">
                  <label className="text-sm font-medium">Max total time (minutes)</label>
                  <div className="relative">
                    <Clock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      type="number"
                      value={maxTotalMinutes}
                      onChange={(e) => {
                        const value = e.target.value;
                        if (value === '') {
                          setMaxTotalMinutes('');
                        } else {
                          const parsed = parseInt(value, 10);
                          if (!isNaN(parsed) && parsed > 0) {
                            setMaxTotalMinutes(parsed);
                          }
                        }
                      }}
                      placeholder="Optional"
                      min="1"
                      className="pl-9"
                    />
                  </div>
                </div>
              </div>
              
              {(searchQuery || maxTotalMinutes !== '') && (
                <Button
                  variant="outline"
                  onClick={() => {
                    setSearchQuery('');
                    setMaxTotalMinutes('');
                  }}
                  className="mt-4"
                >
                  Clear filters
                </Button>
              )}
            </CardContent>
          </Card>

          <div>
            <h2 className="text-xl font-semibold mb-4">
              Showing {filteredDishes.length} of {dishes.length} dishes
            </h2>
            
            {filteredDishes.length === 0 ? (
              <EmptyState
                title={dishes.length === 0 ? "No dishes found" : "No dishes match your search"}
                description={
                  dishes.length === 0
                    ? "Import some dishes to get started."
                    : "Try adjusting your search criteria."
                }
              />
            ) : (
              <div className="grid gap-4">
                {filteredDishes.map((dish) => (
                  <Card key={dish.id} className="hover:shadow-md transition-shadow">
                    <CardContent className="p-6">
                      <div className="flex items-center justify-between">
                        <div className="flex-1">
                          <h3 className="text-lg font-semibold">{dish.name}</h3>
                          <div className="mt-2 flex items-center gap-4 text-sm text-muted-foreground">
                            <span className="flex items-center gap-1">
                              <Clock className="h-4 w-4" />
                              Active: {dish.activeMinutes}min
                            </span>
                            <span className="flex items-center gap-1">
                              <Clock className="h-4 w-4" />
                              Total: {dish.totalMinutes}min
                            </span>
                          </div>
                        </div>
                        <Button variant="outline" asChild>
                          <Link href={`/dishes/${dish.id}`}>
                            Edit
                          </Link>
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
