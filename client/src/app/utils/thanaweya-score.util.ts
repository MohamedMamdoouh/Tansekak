import { DEFAULT_MAXIMUM_SCORE } from '../models';

export function scorePercentage(
  totalDegree: number,
  maxScore: number = DEFAULT_MAXIMUM_SCORE,
): string {
  return ((totalDegree / maxScore) * 100).toFixed(2);
}

export function scoreProgress(
  totalDegree: number,
  maxScore: number = DEFAULT_MAXIMUM_SCORE,
): number {
  return Math.min(Math.max((totalDegree / maxScore) * 100, 0), 100);
}
