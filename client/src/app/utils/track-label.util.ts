import { TRACK_LABELS } from '../models';

export function getTrackLabel(track: string): string {
  return TRACK_LABELS[track] ?? track;
}
