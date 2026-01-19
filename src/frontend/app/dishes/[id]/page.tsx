'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';

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
      <div style={{ padding: '2rem', maxWidth: '800px', margin: '0 auto' }}>
        <p>Loading dish...</p>
      </div>
    );
  }

  if (error && !name) {
    return (
      <div style={{ padding: '2rem', maxWidth: '800px', margin: '0 auto' }}>
        <div style={{ marginBottom: '2rem' }}>
          <Link href="/dishes" style={{ color: 'blue', textDecoration: 'underline' }}>
            ← Back to Dishes
          </Link>
        </div>
        <p style={{ color: 'red' }}>{error}</p>
      </div>
    );
  }

  return (
    <div style={{ padding: '2rem', maxWidth: '800px', margin: '0 auto' }}>
      <div style={{ marginBottom: '2rem' }}>
        <Link href="/dishes" style={{ color: 'blue', textDecoration: 'underline' }}>
          ← Back to Dishes
        </Link>
      </div>

      <h1>Edit Dish</h1>

      {success && (
        <div
          style={{
            padding: '1rem',
            marginBottom: '1rem',
            backgroundColor: '#d4edda',
            color: '#155724',
            border: '1px solid #c3e6cb',
            borderRadius: '4px',
          }}
        >
          ✓ Dish updated successfully!
        </div>
      )}

      {error && (
        <div
          style={{
            padding: '1rem',
            marginBottom: '1rem',
            backgroundColor: '#f8d7da',
            color: '#721c24',
            border: '1px solid #f5c6cb',
            borderRadius: '4px',
          }}
        >
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
            Name *
          </label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            style={{
              width: '100%',
              padding: '0.5rem',
              border: '1px solid #ccc',
              borderRadius: '4px',
            }}
          />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.5rem' }}>
          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
              Active Minutes *
            </label>
            <input
              type="number"
              value={activeMinutes}
              onChange={(e) => setActiveMinutes(parseInt(e.target.value) || 0)}
              required
              min="0"
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
              Total Minutes *
            </label>
            <input
              type="number"
              value={totalMinutes}
              onChange={(e) => setTotalMinutes(parseInt(e.target.value) || 0)}
              required
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

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.5rem' }}>
          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
              Kid Rating (1-5) *
            </label>
            <input
              type="number"
              value={kidRating}
              onChange={(e) => setKidRating(parseInt(e.target.value) || 1)}
              required
              min="1"
              max="5"
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
              Family Rating (1-5) *
            </label>
            <input
              type="number"
              value={familyRating}
              onChange={(e) => setFamilyRating(parseInt(e.target.value) || 1)}
              required
              min="1"
              max="5"
              style={{
                width: '100%',
                padding: '0.5rem',
                border: '1px solid #ccc',
                borderRadius: '4px',
              }}
            />
          </div>
        </div>

        <div style={{ marginBottom: '1.5rem' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={isPescetarian}
              onChange={(e) => setIsPescetarian(e.target.checked)}
            />
            <span>Is Pescetarian</span>
          </label>
        </div>

        <div style={{ marginBottom: '1.5rem' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={hasOptionalMeatVariant}
              onChange={(e) => setHasOptionalMeatVariant(e.target.checked)}
            />
            <span>Has Optional Meat Variant</span>
          </label>
        </div>

        <div style={{ marginBottom: '1.5rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
            <h2 style={{ margin: 0 }}>Ingredients</h2>
            <button
              type="button"
              onClick={addIngredient}
              style={{
                padding: '0.5rem 1rem',
                backgroundColor: '#28a745',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              + Add Ingredient
            </button>
          </div>

          {ingredients.length === 0 && (
            <p style={{ color: '#666', fontStyle: 'italic' }}>
              No ingredients yet. Click "Add Ingredient" to add one.
            </p>
          )}

          {ingredients.map((ingredient, index) => (
            <div
              key={index}
              style={{
                border: '1px solid #ccc',
                borderRadius: '4px',
                padding: '1rem',
                marginBottom: '1rem',
              }}
            >
              <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.9rem', marginBottom: '0.25rem' }}>
                    Name *
                  </label>
                  <input
                    type="text"
                    value={ingredient.name}
                    onChange={(e) => updateIngredient(index, 'name', e.target.value)}
                    required
                    style={{
                      width: '100%',
                      padding: '0.5rem',
                      border: '1px solid #ccc',
                      borderRadius: '4px',
                    }}
                  />
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.9rem', marginBottom: '0.25rem' }}>
                    Category
                  </label>
                  <input
                    type="text"
                    value={ingredient.category}
                    onChange={(e) => updateIngredient(index, 'category', e.target.value)}
                    style={{
                      width: '100%',
                      padding: '0.5rem',
                      border: '1px solid #ccc',
                      borderRadius: '4px',
                    }}
                  />
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.9rem', marginBottom: '0.25rem' }}>
                    Amount *
                  </label>
                  <input
                    type="number"
                    value={ingredient.amount}
                    onChange={(e) => updateIngredient(index, 'amount', parseFloat(e.target.value) || 0)}
                    required
                    min="0.01"
                    step="0.01"
                    style={{
                      width: '100%',
                      padding: '0.5rem',
                      border: '1px solid #ccc',
                      borderRadius: '4px',
                    }}
                  />
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.9rem', marginBottom: '0.25rem' }}>
                    Unit *
                  </label>
                  <input
                    type="text"
                    value={ingredient.unit}
                    onChange={(e) => updateIngredient(index, 'unit', e.target.value)}
                    required
                    style={{
                      width: '100%',
                      padding: '0.5rem',
                      border: '1px solid #ccc',
                      borderRadius: '4px',
                    }}
                  />
                </div>

                <div style={{ display: 'flex', alignItems: 'flex-end' }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={ingredient.optional}
                      onChange={(e) => updateIngredient(index, 'optional', e.target.checked)}
                    />
                    <span style={{ fontSize: '0.9rem' }}>Optional</span>
                  </label>
                </div>
              </div>

              <button
                type="button"
                onClick={() => removeIngredient(index)}
                style={{
                  padding: '0.25rem 0.75rem',
                  backgroundColor: '#dc3545',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '0.9rem',
                }}
              >
                Remove
              </button>
            </div>
          ))}
        </div>

        <div style={{ display: 'flex', gap: '1rem' }}>
          <button
            type="submit"
            disabled={saving}
            style={{
              padding: '0.75rem 2rem',
              backgroundColor: saving ? '#ccc' : '#007bff',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: saving ? 'not-allowed' : 'pointer',
              fontSize: '1rem',
              fontWeight: 'bold',
            }}
          >
            {saving ? 'Saving...' : 'Save Changes'}
          </button>

          <Link
            href="/dishes"
            style={{
              display: 'inline-block',
              padding: '0.75rem 2rem',
              backgroundColor: '#6c757d',
              color: 'white',
              textDecoration: 'none',
              borderRadius: '4px',
              fontSize: '1rem',
              fontWeight: 'bold',
            }}
          >
            Cancel
          </Link>
        </div>
      </form>
    </div>
  );
}
