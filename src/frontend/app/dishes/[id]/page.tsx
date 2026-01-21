'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { PageTitle, FormRow } from '@/components/ui-primitives';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Loader2, CheckCircle2, XCircle, Trash2, Plus } from 'lucide-react';

interface Ingredient {
  name: string;
  category: string;
  amount: number;
  unit: string;
  optional: boolean;
}

interface DishDetails {
  id: string;
  name: string;
  activeMinutes: number;
  totalMinutes: number;
  kidRating: number;
  familyRating: number;
  isPescetarian: boolean;
  hasOptionalMeatVariant: boolean;
  ingredients: Ingredient[];
}

export default function EditDishPage() {
  const params = useParams();
  const router = useRouter();
  const dishId = params.id as string;
  
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  
  const [name, setName] = useState('');
  const [activeMinutes, setActiveMinutes] = useState(0);
  const [totalMinutes, setTotalMinutes] = useState(0);
  const [kidRating, setKidRating] = useState(3);
  const [familyRating, setFamilyRating] = useState(3);
  const [isPescetarian, setIsPescetarian] = useState(false);
  const [hasOptionalMeatVariant, setHasOptionalMeatVariant] = useState(false);
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);

  const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';

  useEffect(() => {
    fetchDishDetails();
  }, [dishId]);

  const fetchDishDetails = async () => {
    try {
      const response = await fetch(`${apiUrl}/dishes/${dishId}`);
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error('Dish not found');
        }
        throw new Error(`Failed to fetch dish: ${response.status}`);
      }
      const data: DishDetails = await response.json();
      
      setName(data.name);
      setActiveMinutes(data.activeMinutes);
      setTotalMinutes(data.totalMinutes);
      setKidRating(data.kidRating);
      setFamilyRating(data.familyRating);
      setIsPescetarian(data.isPescetarian);
      setHasOptionalMeatVariant(data.hasOptionalMeatVariant);
      setIngredients(data.ingredients);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load dish');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setSuccess(false);
    setError(null);

    try {
      const response = await fetch(`${apiUrl}/dishes/${dishId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name,
          activeMinutes,
          totalMinutes,
          kidRating,
          familyRating,
          isPescetarian,
          hasOptionalMeatVariant,
          ingredients,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || `Update failed: ${response.status}`);
      }

      setSuccess(true);
      // Refetch to ensure we show the saved state
      await fetchDishDetails();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update dish');
    } finally {
      setSaving(false);
    }
  };

  const addIngredient = () => {
    setIngredients([
      ...ingredients,
      { name: '', category: '', amount: 1, unit: '', optional: false },
    ]);
  };

  const removeIngredient = (index: number) => {
    setIngredients(ingredients.filter((_, i) => i !== index));
  };

  const updateIngredient = (index: number, field: keyof Ingredient, value: any) => {
    const updated = [...ingredients];
    updated[index] = { ...updated[index], [field]: value };
    setIngredients(updated);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        <span className="ml-2 text-muted-foreground">Loading dish...</span>
      </div>
    );
  }

  if (error && !name) {
    return (
      <div className="space-y-4">
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
        <Button variant="outline" asChild>
          <Link href="/dishes">Back to Dishes</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <PageTitle>Edit Dish</PageTitle>
        <Button variant="outline" asChild>
          <Link href="/dishes">Back to Dishes</Link>
        </Button>
      </div>

      {success && (
        <Alert className="border-green-200 bg-green-50">
          <CheckCircle2 className="h-4 w-4 text-green-600" />
          <AlertTitle className="text-green-900">Success!</AlertTitle>
          <AlertDescription className="text-green-900">
            Dish updated successfully!
          </AlertDescription>
        </Alert>
      )}

      {error && (
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <form onSubmit={handleSubmit} className="space-y-8">
        <Card>
          <CardHeader>
            <CardTitle>Basic Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <FormRow label="Name" required>
              <Input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </FormRow>

            <div className="grid gap-4 md:grid-cols-2">
              <FormRow label="Active Minutes" required>
                <Input
                  type="number"
                  value={activeMinutes}
                  onChange={(e) => setActiveMinutes(parseInt(e.target.value) || 0)}
                  required
                  min="0"
                />
              </FormRow>

              <FormRow label="Total Minutes" required>
                <Input
                  type="number"
                  value={totalMinutes}
                  onChange={(e) => setTotalMinutes(parseInt(e.target.value) || 0)}
                  required
                  min="1"
                />
              </FormRow>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <FormRow label="Kid Rating (1-5)" required>
                <Input
                  type="number"
                  value={kidRating}
                  onChange={(e) => setKidRating(parseInt(e.target.value) || 1)}
                  required
                  min="1"
                  max="5"
                />
              </FormRow>

              <FormRow label="Family Rating (1-5)" required>
                <Input
                  type="number"
                  value={familyRating}
                  onChange={(e) => setFamilyRating(parseInt(e.target.value) || 1)}
                  required
                  min="1"
                  max="5"
                />
              </FormRow>
            </div>

            <div className="space-y-2">
              <Label className="flex items-center gap-2 cursor-pointer">
                <Checkbox
                  checked={isPescetarian}
                  onChange={(e) => setIsPescetarian(e.target.checked)}
                />
                <span>Is Pescetarian</span>
              </Label>

              <Label className="flex items-center gap-2 cursor-pointer">
                <Checkbox
                  checked={hasOptionalMeatVariant}
                  onChange={(e) => setHasOptionalMeatVariant(e.target.checked)}
                />
                <span>Has Optional Meat Variant</span>
              </Label>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle>Ingredients</CardTitle>
            <Button type="button" onClick={addIngredient} size="sm">
              <Plus className="h-4 w-4 mr-2" />
              Add Ingredient
            </Button>
          </CardHeader>
          <CardContent className="space-y-4">
            {ingredients.length === 0 && (
              <p className="text-sm text-muted-foreground italic text-center py-4">
                No ingredients yet. Click "Add Ingredient" to add one.
              </p>
            )}

            {ingredients.map((ingredient, index) => (
              <Card key={index} className="bg-muted/50">
                <CardContent className="pt-6 space-y-4">
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label>Name *</Label>
                      <Input
                        type="text"
                        value={ingredient.name}
                        onChange={(e) => updateIngredient(index, 'name', e.target.value)}
                        required
                      />
                    </div>

                    <div className="space-y-2">
                      <Label>Category</Label>
                      <Input
                        type="text"
                        value={ingredient.category}
                        onChange={(e) => updateIngredient(index, 'category', e.target.value)}
                      />
                    </div>
                  </div>

                  <div className="grid gap-4 md:grid-cols-3">
                    <div className="space-y-2">
                      <Label>Amount *</Label>
                      <Input
                        type="number"
                        value={ingredient.amount}
                        onChange={(e) => updateIngredient(index, 'amount', parseFloat(e.target.value) || 0)}
                        required
                        min="0.01"
                        step="0.01"
                      />
                    </div>

                    <div className="space-y-2">
                      <Label>Unit *</Label>
                      <Input
                        type="text"
                        value={ingredient.unit}
                        onChange={(e) => updateIngredient(index, 'unit', e.target.value)}
                        required
                      />
                    </div>

                    <div className="flex items-end">
                      <Label className="flex items-center gap-2 cursor-pointer pb-2">
                        <Checkbox
                          checked={ingredient.optional}
                          onChange={(e) => updateIngredient(index, 'optional', e.target.checked)}
                        />
                        <span>Optional</span>
                      </Label>
                    </div>
                  </div>

                  <Button
                    type="button"
                    variant="destructive"
                    size="sm"
                    onClick={() => removeIngredient(index)}
                  >
                    <Trash2 className="h-4 w-4 mr-2" />
                    Remove
                  </Button>
                </CardContent>
              </Card>
            ))}
          </CardContent>
        </Card>

        <div className="flex gap-4">
          <Button type="submit" disabled={saving}>
            {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            {saving ? 'Saving...' : 'Save Changes'}
          </Button>

          <Button type="button" variant="outline" asChild>
            <Link href="/dishes">Cancel</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
