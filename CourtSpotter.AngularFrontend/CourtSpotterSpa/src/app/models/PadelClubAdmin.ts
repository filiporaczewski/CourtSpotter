import {PadelClub} from './padel-club';

export interface PadelClubAdmin extends PadelClub {
  provider: string,
  pagesCount?: number
}
