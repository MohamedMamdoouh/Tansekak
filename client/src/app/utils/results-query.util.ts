export function isValidResultsQuery(track: string, score: number): boolean {
  return !!track && !Number.isNaN(score);
}

export function parseResultsScore(rawScore: string | undefined | null): number {
  if (rawScore === undefined || rawScore === null || rawScore === '') {
    return Number.NaN;
  }

  return Number(rawScore);
}
