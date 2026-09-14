import { StudentResult } from '../models';
import { canonicalizeTrack } from './track-label.util';

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
    params.track = canonicalizeTrack(result.track);
  }
  return params;
}
