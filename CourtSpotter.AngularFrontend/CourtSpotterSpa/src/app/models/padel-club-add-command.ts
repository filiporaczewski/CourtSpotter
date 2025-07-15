import {ProviderType} from './provider-type';

export interface PadelClubAddCommand {
  name: string,
  provider: ProviderType,
  pagesCount?: number
}
