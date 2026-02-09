export type WeeklyPlanSelectionInput = {
  type: string;
  dishId?: string | null;
};

export type WeeklyPlanDayInput = {
  date?: string | null;
  selection?: WeeklyPlanSelectionInput | null;
};

export type WeeklyPlanUpsertRequest = {
  days?: WeeklyPlanDayInput[] | null;
};

export type WeeklyPlanSelection = {
  type: string;
  dishId?: string | null;
};

export type WeeklyPlanDay = {
  date: string;
  selection: WeeklyPlanSelection;
};

export type WeeklyPlanUpsertResponse = {
  id: string;
  startDate: string;
  days: WeeklyPlanDay[];
};

export type WeeklyPlanResponse = WeeklyPlanUpsertResponse & {
  isMarkedAsEaten: boolean;
};
export type WeeklyPlanGenerateResponse = WeeklyPlanUpsertResponse & {
  notes?: string[];
};

export type WeeklyPlanSummary = {
  startDate: string;
  endDate: string;
};

export type WeeklyPlansResponse = {
  plans: WeeklyPlanSummary[];
};
