# Frontend Changes: Weekly Plan Suggest & Save Endpoints

## Summary
Updated the frontend React/TypeScript code to support the new backend endpoints for suggesting and saving weekly plans.

## Changes Made

### 1. New State Management
Added new state variables to track suggested plans:
- `suggesting`: Boolean to track the "suggest" operation in progress
- `isSuggestedPlan`: Boolean to indicate if the current plan is suggested (not yet saved)

### 2. New API Endpoint: Suggest Plan
Added `handleSuggestPlan()` function that:
- Calls `POST /weekly-plan/suggest` with `{ weekStartDate: string }`
- Receives `{ plan, explanationsByDay, violations }`
- Sets `isSuggestedPlan = true` to indicate the plan is not yet saved
- Displays the plan with explanations
- Shows any violations returned

### 3. Updated Save Functionality
Modified `handleSavePlan()` to:
- Call `POST /weekly-plan/save` instead of `PUT /weekly-plan/{date}`
- Send `{ weekStartDate, items: [{ dayIndex, dishId }] }`
- Clear `isSuggestedPlan` flag on successful save
- Reload the saved plan from the backend

### 4. UI Enhancements

#### New "Suggest Plan" Button
- Placed before "Generate New Plan" button
- Uses secondary variant to distinguish from generate
- Shows loading state with "Suggesting..." text

#### Suggested Plan Banner
When a plan is suggested but not saved, shows a prominent blue alert banner with:
- Title: "Suggested Plan - Not Yet Saved"
- Description explaining the plan is not persisted
- "Save This Plan" button to commit the plan
- "Edit Before Saving" button to make changes first

#### Edit Mode Enhancements
- Title changes to "Edit Suggested Plan" when editing a suggested plan
- Description indicates the plan is "not yet saved"
- Save button commits the plan to the backend

### 5. User Flows

#### Flow 1: Suggest → Save
1. User clicks "Suggest Plan"
2. Backend returns suggested plan with explanations
3. Blue banner appears: "Suggested Plan - Not Yet Saved"
4. User clicks "Save This Plan"
5. Plan is persisted and banner disappears

#### Flow 2: Suggest → Edit → Save
1. User clicks "Suggest Plan"
2. Backend returns suggested plan with explanations
3. User clicks "Edit Before Saving"
4. User modifies dishes for specific days
5. User clicks "Save Plan"
6. Modified plan is persisted

#### Flow 3: Generate (existing behavior)
1. User clicks "Generate New Plan"
2. Backend generates and immediately persists the plan
3. Plan is displayed (no "suggested" banner)

### 6. Type Updates
- Added `violations?: RuleViolation[]` to `GenerateWeeklyPlanResponse` interface
- Both suggest and generate responses now support violations

## Files Modified
- `/src/frontend/app/weekly-plan/page.tsx`

## Testing
- Frontend builds successfully with TypeScript compilation
- No build errors or warnings
- All imports and types are valid

## Next Steps
1. Test the UI in a browser with the backend endpoints
2. Verify the suggest/save flow works end-to-end
3. Confirm explanations are displayed correctly
4. Test violations handling for both suggest and save
5. Verify the "Generate New Plan" flow still works (unchanged behavior)
