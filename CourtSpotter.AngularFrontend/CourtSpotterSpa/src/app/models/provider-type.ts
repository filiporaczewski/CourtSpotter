export enum ProviderType {
  Playtomic,
  CourtMe,
  KlubyOrg,
  RezerwujKort
}

export interface ProviderTypeOption {
  value: ProviderType;
  displayValue: string;
}

export const PROVIDER_TYPE_OPTIONS: ProviderTypeOption[] = [
  { value: ProviderType.Playtomic, displayValue: 'Playtomic' },
  { value: ProviderType.CourtMe, displayValue: 'CourtMe' },
  { value: ProviderType.KlubyOrg, displayValue: 'Kluby.org' },
  { value: ProviderType.RezerwujKort, displayValue: 'Rezerwuj Kort' }
];
