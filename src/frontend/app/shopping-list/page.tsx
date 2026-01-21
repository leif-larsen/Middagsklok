'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { Calendar, ShoppingCart, Loader2, AlertCircle, ChevronRight } from 'lucide-react';
import { PageTitle, EmptyState } from '@/components/ui-primitives';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';

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
    <div className="container mx-auto max-w-5xl px-4 py-8">
      <div className="mb-6">
        <Link 
          href="/" 
          className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ChevronRight className="h-4 w-4 mr-1 rotate-180" />
          Back to Home
        </Link>
      </div>

      <PageTitle className="mb-8 flex items-center gap-2">
        <ShoppingCart className="h-8 w-8" />
        Handleliste
      </PageTitle>

      <Card className="mb-8">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Calendar className="h-5 w-5" />
            Velg uke
          </CardTitle>
          <CardDescription>
            Velg hvilken uke du vil se handlelisten for
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-end">
            <div className="space-y-2 flex-1">
              <Label htmlFor="week-start">Ukestart (mandag)</Label>
              <Input
                id="week-start"
                type="date"
                value={weekStart}
                onChange={(e) => setWeekStart(e.target.value)}
                className="max-w-xs"
              />
            </div>
            <Button
              variant="outline"
              onClick={() => setWeekStart(getMonday(new Date()))}
            >
              <Calendar className="h-4 w-4 mr-2" />
              Bruk denne uken
            </Button>
          </div>

          <div className="mt-4">
            <Button
              onClick={handleLoadShoppingList}
              disabled={loading}
              className="w-full sm:w-auto"
            >
              {loading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Laster...
                </>
              ) : (
                <>
                  <ShoppingCart className="h-4 w-4 mr-2" />
                  Last handleliste
                </>
              )}
            </Button>
          </div>
        </CardContent>
      </Card>

      {error && (
        <Alert variant="destructive" className="mb-8">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            <strong>Feil:</strong> {error}
          </AlertDescription>
        </Alert>
      )}

      {loading && !shoppingList && (
        <Card>
          <CardContent className="py-12">
            <div className="flex flex-col items-center justify-center">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground mb-4" />
              <p className="text-sm text-muted-foreground">Laster handleliste...</p>
            </div>
          </CardContent>
        </Card>
      )}

      {shoppingList && !loading && (
        <Card>
          <CardHeader>
            <CardTitle>Handleliste for uke {shoppingList.weekStartDate}</CardTitle>
            <CardDescription>
              {categories.length > 0 && (
                <>
                  {shoppingList.items.length} varer i {categories.length} kategorier
                </>
              )}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {categories.length === 0 ? (
              <EmptyState
                icon={<ShoppingCart className="h-12 w-12" />}
                title="Ingen varer på handlelisten"
                description="Denne uken har ingen varer på handlelisten. Generer en ukesplan for å lage en handleliste."
              />
            ) : (
              <div className="space-y-8">
                {categories.map((category) => (
                  <div key={category}>
                    <div className="flex items-center gap-2 mb-3">
                      <h3 className="text-lg font-semibold">
                        {category}
                      </h3>
                      <Badge variant="secondary" className="ml-auto">
                        {groupedItems[category].length}
                      </Badge>
                    </div>
                    <div className="rounded-md border">
                      <ul className="divide-y">
                        {groupedItems[category].map((item, idx) => (
                          <li
                            key={idx}
                            className="flex items-center justify-between px-4 py-3 hover:bg-muted/50 transition-colors"
                          >
                            <span className="font-medium text-sm">
                              {item.ingredientName}
                            </span>
                            <span className="text-sm text-muted-foreground">
                              {item.amount} {item.unit}
                            </span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
