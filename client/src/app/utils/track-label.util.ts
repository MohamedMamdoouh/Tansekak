import { TRACK_LABELS } from '../models';

const TRACK_ALIASES: Record<string, string> = {
  Science: 'Science',
  Mathematics: 'Mathematics',
  Literature: 'Literature',
  'علمي علوم': 'Science',
  'علمي رياضة': 'Mathematics',
  'علمي رياضه': 'Mathematics',
  أدبي: 'Literature',
  ادبي: 'Literature',
  الادبي: 'Literature',
};

export function canonicalizeTrack(track: string | null | undefined): string {
  if (!track) return '';
  return TRACK_ALIASES[track] ?? TRACK_ALIASES[track.trim()] ?? track;
}

export function getTrackLabel(track: string): string {
  const canonical = canonicalizeTrack(track);
  return TRACK_LABELS[canonical] ?? TRACK_LABELS[track] ?? track;
}
