import {ApplicationConfig, isDevMode, provideZoneChangeDetection} from '@angular/core';
import {provideRouter} from '@angular/router';

import {routes} from './app.routes';
import {HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi} from '@angular/common/http';
import {TranslocoHttpLoader} from './transloco-loader';
import {provideTransloco} from '@jsverse/transloco';
import {provideTranslocoLocale} from '@jsverse/transloco-locale';
import {provideTranslocoPersistLang} from '@jsverse/transloco-persist-lang';
import {
  BrowserCacheLocation,
  InteractionType,
  IPublicClientApplication,
  LogLevel,
  PublicClientApplication
} from '@azure/msal-browser';
import {environment} from '../environments/environment';
import {
  MSAL_GUARD_CONFIG,
  MSAL_INSTANCE,
  MSAL_INTERCEPTOR_CONFIG,
  MsalBroadcastService,
  MsalGuard,
  MsalGuardConfiguration,
  MsalInterceptor,
  MsalInterceptorConfiguration,
  MsalService,
  ProtectedResourceScopes
} from '@azure/msal-angular';

export function MSALInstanceFactory() : IPublicClientApplication {
  return new PublicClientApplication({
    auth: environment.auth,
    cache: { cacheLocation: BrowserCacheLocation.LocalStorage },
    system: { loggerOptions: { loggerCallback: () => {}, logLevel: LogLevel.Info } }
  });
 }

 export function MSALGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: { scopes: environment.api_scopes }
  }
 }

 export function MSALInterceptorConfigFactory(): MsalInterceptorConfiguration {
   const protectedResourceMap = new Map<string, Array<ProtectedResourceScopes>>();

   protectedResourceMap.set(`${environment.apiBaseUrl}/padel-clubs`, [
     {
       httpMethod: 'POST',
       scopes: environment.api_scopes
     }
   ]);

   return {
     interactionType: InteractionType.Redirect,
     protectedResourceMap
   }
 }

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    provideTransloco({
      config: {
          availableLangs: ['en', 'pl'],
          defaultLang: 'en',
          // Remove this option if your application doesn't support changing language in runtime.
          reRenderOnLangChange: true,
          prodMode: !isDevMode(),
        },
        loader: TranslocoHttpLoader
      }),
    provideTranslocoLocale(),
    provideTranslocoPersistLang({
      storage: {
        useValue: localStorage
      }
    }),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: MsalInterceptor,
      multi: true
    },
    {
      provide: MSAL_INSTANCE,
      useFactory: MSALInstanceFactory
    },
    {
      provide: MSAL_INTERCEPTOR_CONFIG,
      useFactory: MSALInterceptorConfigFactory
    },
    {
      provide: MSAL_GUARD_CONFIG,
      useFactory: MSALGuardConfigFactory
    },
    MsalService,
    MsalGuard,
    MsalBroadcastService
  ]
};
