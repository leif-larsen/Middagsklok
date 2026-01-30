export type PlanningSettingsRequest = {
  weekStartsOn?: string | null;
};

export type PlanningSettingsResponse = {
  id: string;
  weekStartsOn: string;
};
