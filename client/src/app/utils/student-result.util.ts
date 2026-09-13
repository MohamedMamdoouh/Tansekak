import { StudentResult } from '../models';

export function hasTrackRank(result: StudentResult): boolean {
  return result.trackRank != null && result.trackTotalStudents != null;
}

export function predictQueryParams(
  result: StudentResult | null,
): { score: number; track?: string } {
  if (!result) return { score: 0 };
  const params: { score: number; track?: string } = {
    score: result.totalDegree,
  };
  if (result.track) {
    params.track = result.track;
  }
  return params;
}
