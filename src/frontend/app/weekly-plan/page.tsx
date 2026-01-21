'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select } from '@/components/ui/select';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { PageTitle } from '@/components/ui-primitives';
import { Loader2, AlertCircle, CalendarDays, ChevronLeft, Save, X, Edit, Sparkles, Clock } from 'lucide-react';

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
  violations?: RuleViolation[];
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
  const [suggesting, setSuggesting] = useState(false);
  const [dishes, setDishes] = useState<Dish[]>([]);
  const [editMode, setEditMode] = useState(false);
  const [editedPlan, setEditedPlan] = useState<{ [dayIndex: number]: string }>({});
  const [saving, setSaving] = useState(false);
  const [violations, setViolations] = useState<RuleViolation[]>([]);
  const [isSuggestedPlan, setIsSuggestedPlan] = useState(false);

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
    setIsSuggestedPlan(false);

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
      setIsSuggestedPlan(false);
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
      setIsSuggestedPlan(false);
      initializeEditedPlanFromExisting(result.plan);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate plan');
    } finally {
      setGenerating(false);
    }
  };

  const handleSuggestPlan = async () => {
    const dateStr = weekStart || getMonday(new Date());
    
    setSuggesting(true);
    setError(null);
    setViolations([]);

    try {
      const response = await fetch(`${apiUrl}/weekly-plan/suggest`, {
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
        throw new Error(errorData.error || `Failed to suggest plan: ${response.status}`);
      }

      const result: GenerateWeeklyPlanResponse = await response.json();
      setPlan(result.plan);
      setExplanations(result.explanationsByDay);
      setWeekStart(dateStr);
      setEditMode(false);
      setIsSuggestedPlan(true);
      setViolations(result.violations || []);
      initializeEditedPlanFromExisting(result.plan);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to suggest plan');
    } finally {
      setSuggesting(false);
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

      const response = await fetch(`${apiUrl}/weekly-plan/save`, {
        method: 'POST',
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
      setIsSuggestedPlan(false);
      await loadPlanForWeek(dateStr);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save plan');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="container mx-auto px-4 py-8 max-w-7xl">
      <div className="mb-6">
        <Link href="/" className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors">
          <ChevronLeft className="mr-1 h-4 w-4" />
          Back to Home
        </Link>
      </div>

      <PageTitle className="mb-8">Weekly Plan</PageTitle>

      <Card className="mb-8">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CalendarDays className="h-5 w-5" />
            Select Week
          </CardTitle>
          <CardDescription>Choose a week to view or create a meal plan</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1 space-y-2">
              <Label htmlFor="week-start">Week start (Monday)</Label>
              <Input
                id="week-start"
                type="date"
                value={weekStart}
                onChange={(e) => handleWeekChange(e.target.value)}
                className="max-w-xs"
              />
            </div>
            <div className="flex items-end">
              <Button
                onClick={() => handleWeekChange(getMonday(new Date()))}
                variant="outline"
              >
                Use This Week
              </Button>
            </div>
          </div>

          <div className="flex flex-wrap gap-3">
            <Button
              onClick={handleSuggestPlan}
              disabled={suggesting}
              variant="secondary"
              size="lg"
            >
              {suggesting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Suggesting...
                </>
              ) : (
                <>
                  <Sparkles className="mr-2 h-4 w-4" />
                  Suggest Plan
                </>
              )}
            </Button>

            <Button
              onClick={handleGeneratePlan}
              disabled={generating}
              size="lg"
            >
              {generating ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Generating...
                </>
              ) : (
                <>
                  <Sparkles className="mr-2 h-4 w-4" />
                  Generate New Plan
                </>
              )}
            </Button>

            {plan && !editMode && (
              <Button
                onClick={handleEditPlan}
                variant="secondary"
                size="lg"
              >
                <Edit className="mr-2 h-4 w-4" />
                Edit Plan
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {error && (
        <Alert variant="destructive" className="mb-8">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {violations.length > 0 && (
        <Alert className="mb-8 border-orange-500 bg-orange-50 dark:bg-orange-950/20">
          <AlertCircle className="h-4 w-4 text-orange-600" />
          <AlertTitle className="text-orange-800 dark:text-orange-400">Validation Errors</AlertTitle>
          <AlertDescription className="mt-2 space-y-2">
            {violations.map((violation, idx) => (
              <div key={idx} className="text-sm">
                <span className="font-semibold text-orange-900 dark:text-orange-300">
                  {violation.ruleCode}:
                </span>{' '}
                <span className="text-orange-800 dark:text-orange-400">{violation.message}</span>
                {violation.dayIndices.length > 0 && (
                  <div className="mt-1 flex flex-wrap gap-1">
                    {violation.dayIndices.map((d) => (
                      <Badge key={d} variant="outline" className="text-xs border-orange-300 text-orange-700">
                        {dayNames[d]}
                      </Badge>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </AlertDescription>
        </Alert>
      )}

      {isSuggestedPlan && plan && !editMode && (
        <Alert className="mb-8 border-blue-500 bg-blue-50 dark:bg-blue-950/20">
          <Sparkles className="h-4 w-4 text-blue-600" />
          <AlertTitle className="text-blue-800 dark:text-blue-400">Suggested Plan - Not Yet Saved</AlertTitle>
          <AlertDescription className="mt-2">
            <p className="text-sm text-blue-800 dark:text-blue-400 mb-3">
              This is a suggested meal plan with AI-generated explanations. The plan has not been saved yet.
            </p>
            <div className="flex gap-2">
              <Button
                onClick={handleSavePlan}
                disabled={saving}
                size="default"
              >
                {saving ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    <Save className="mr-2 h-4 w-4" />
                    Save This Plan
                  </>
                )}
              </Button>
              <Button
                onClick={handleEditPlan}
                variant="outline"
              >
                <Edit className="mr-2 h-4 w-4" />
                Edit Before Saving
              </Button>
            </div>
          </AlertDescription>
        </Alert>
      )}

      {editMode && (
        <Card className="mb-8">
          <CardHeader>
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
              <div>
                <CardTitle>
                  {isSuggestedPlan ? 'Edit Suggested Plan' : 'Edit Weekly Plan'}
                </CardTitle>
                <CardDescription className="mt-1.5">
                  {isSuggestedPlan 
                    ? `Editing suggested plan for week of ${weekStart} (not yet saved)`
                    : `Planning for week of ${weekStart}`
                  }
                </CardDescription>
              </div>
              <div className="flex gap-2">
                <Button
                  onClick={handleSavePlan}
                  disabled={saving}
                  size="default"
                >
                  {saving ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Saving...
                    </>
                  ) : (
                    <>
                      <Save className="mr-2 h-4 w-4" />
                      Save Plan
                    </>
                  )}
                </Button>
                <Button
                  onClick={handleCancelEdit}
                  disabled={saving}
                  variant="outline"
                >
                  <X className="mr-2 h-4 w-4" />
                  Cancel
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[0, 1, 2, 3, 4, 5, 6].map((dayIndex) => {
                const selectedDish = dishes.find(d => d.id === editedPlan[dayIndex]);
                return (
                  <Card key={dayIndex} className="bg-muted/50">
                    <CardContent className="pt-6">
                      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 items-start">
                        <div className="lg:col-span-2">
                          <Label className="text-base font-semibold">
                            {dayNames[dayIndex]}
                          </Label>
                        </div>
                        <div className="lg:col-span-7 space-y-2">
                          <Select
                            value={editedPlan[dayIndex] || ''}
                            onChange={(e) => handleDishChange(dayIndex, e.target.value)}
                          >
                            <option value="">-- Select a dish --</option>
                            {dishes.map(dish => (
                              <option key={dish.id} value={dish.id}>
                                {dish.name} ({dish.totalMinutes}min)
                              </option>
                            ))}
                          </Select>
                        </div>
                        {selectedDish && (
                          <div className="lg:col-span-3 flex items-center gap-3 text-sm text-muted-foreground">
                            <div className="flex items-center gap-1">
                              <Clock className="h-3 w-3" />
                              <span>Active: {selectedDish.activeMinutes}m</span>
                            </div>
                            <div className="flex items-center gap-1">
                              <Clock className="h-3 w-3" />
                              <span>Total: {selectedDish.totalMinutes}m</span>
                            </div>
                          </div>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          </CardContent>
        </Card>
      )}

      {plan && !editMode && (
        <div className="space-y-6">
          <div>
            <h2 className="text-2xl font-bold tracking-tight mb-4">
              Weekly Plan for {plan.weekStartDate}
            </h2>
          </div>

          <div className="space-y-4">
            {plan.items
              .sort((a, b) => a.dayIndex - b.dayIndex)
              .map((item) => (
                <Card key={item.dayIndex}>
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div className="space-y-1 flex-1">
                        <CardTitle className="flex items-center gap-3">
                          <Badge variant="outline" className="text-sm font-normal">
                            {dayNames[item.dayIndex]}
                          </Badge>
                          <span>{item.dish.name}</span>
                        </CardTitle>
                        <div className="flex items-center gap-4 text-sm text-muted-foreground">
                          <div className="flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            <span>Active: {item.dish.activeMinutes}min</span>
                          </div>
                          <div className="flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            <span>Total: {item.dish.totalMinutes}min</span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </CardHeader>
                  {explanations && explanations[item.dayIndex] && (
                    <CardContent>
                      <div className="rounded-lg bg-muted/50 p-4">
                        <p className="text-sm font-semibold mb-2 text-foreground">
                          Why this dish:
                        </p>
                        <ul className="space-y-1 text-sm text-muted-foreground">
                          {explanations[item.dayIndex].reasons.map((reason, idx) => (
                            <li key={idx} className="flex items-start gap-2">
                              <span className="text-primary mt-0.5">•</span>
                              <span>{reason}</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    </CardContent>
                  )}
                </Card>
              ))}
          </div>
        </div>
      )}

      {loading && (
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
              <span className="ml-3 text-muted-foreground">Loading plan...</span>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
