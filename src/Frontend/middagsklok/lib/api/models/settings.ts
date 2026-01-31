export type PlanningSettingsRequest = {
  weekStartsOn?: string | null;
  seafoodPerWeek?: number | null;
};

export type PlanningSettingsResponse = {
  id: string;
  weekStartsOn: string;
  seafoodPerWeek: number;
};
