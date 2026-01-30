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
