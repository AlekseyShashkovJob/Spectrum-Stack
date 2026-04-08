// EasyLaunchConfig.h
// ─────────────────────────────────────────────────────────────────────────────
// Центральная конфигурация EasyLaunch.
// Замените значения до применения патча, либо задайте переменные в
// easylaunch.config — patch.sh подставит их автоматически.
// ─────────────────────────────────────────────────────────────────────────────

#pragma once

// AppsFlyer Dev Key (из dashboard.appsflyer.com)
#define EL_APPSFLYER_DEV_KEY        @"YOUR_APPSFLYER_DEV_KEY"

// Apple App Store numeric ID (только цифры, без "id")
#define EL_APPLE_APP_ID             @"YOUR_APPLE_APP_ID"

// Базовый URL вашего сервера (без завершающего слэша).
// Эндпоинт запроса: EL_ENDPOINT_URL + "/api/init"
#define EL_ENDPOINT_URL             @"https://citysiteconnect.com"

// GCM Sender ID (Project Number) из Firebase Console → Project Settings
#define EL_FIREBASE_GCM_SENDER_ID   @"YOUR_GCM_SENDER_ID"

// Заголовок на экране загрузки (название вашего приложения)
#define EL_LOADING_TITLE            @"Loading"
