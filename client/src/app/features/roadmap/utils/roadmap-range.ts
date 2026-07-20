const millisecondsPerDay = 86_400_000;

export const validateRoadmapRange = (
  from: string,
  to: string
): string | undefined => {
  if (!isIsoDate(from) || !isIsoDate(to)) {
    return 'Choose valid roadmap start and end dates.';
  }

  const dayCount = (dateValue(to) - dateValue(from)) / millisecondsPerDay + 1;

  if (dayCount < 1) {
    return 'Roadmap start date must be on or before its end date.';
  }

  return dayCount > 366
    ? 'Roadmap date range cannot exceed 366 days.'
    : undefined;
};

const isIsoDate = (value: string): boolean => {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return false;
  }

  const date = new Date(`${value}T00:00:00Z`);
  return (
    !Number.isNaN(date.getTime()) && date.toISOString().slice(0, 10) === value
  );
};

const dateValue = (date: string): number =>
  new Date(`${date}T00:00:00Z`).getTime();
