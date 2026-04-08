#import "PreloadViewController.h"
#import "PLServicesWrapper.h"        // Firebase + AppsFlyer bridge (.m, pure ObjC)
#import <UserNotifications/UserNotifications.h>
#import "NotificationPromptViewController.h"

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - PreloadConfig
// ─────────────────────────────────────────────────────────────────────────────

@implementation PreloadConfig

- (instancetype)init
{
    self = [super init];
    if (self) {
        _appsflyerTimeout = 15.0;
        _endpointTimeout  = 10.0;
    }
    return self;
}

+ (instancetype)configWithAppsDevKey:(NSString *)devKey
                          appleAppId:(NSString *)appleId
                         endpointURL:(NSString *)endpoint
                  firebaseGCMSenderId:(NSString *)gcmSenderId
{
    PreloadConfig *c      = [PreloadConfig new];
    c.appsDevKey          = devKey;
    c.appleAppId          = appleId;
    c.endpointURL         = endpoint;
    c.firebaseGCMSenderId = gcmSenderId;
    return c;
}

@end

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - PreloadViewController private interface
// ─────────────────────────────────────────────────────────────────────────────

@interface PreloadViewController ()

/// Фоновое изображение
@property (nonatomic, strong) UIImageView              *backgroundImageView;

/// Логотип приложения
@property (nonatomic, strong) UIImageView              *logoImageView;

/// Спиннер — крутится всё время загрузки
@property (nonatomic, strong) UIActivityIndicatorView  *spinner;

/// Собранные данные атрибуции для передачи на эндпоинт
@property (nonatomic, strong, nullable) NSDictionary *attributionData;
// Guard to avoid presenting the custom notification prompt multiple times within the same call
@property (atomic, assign) BOOL isPresentingNotificationPrompt;
// Флаг сессии: уведомления уже спрашивались в рамках текущего запуска приложения (in-memory, не персистируется)
@property (atomic, assign) BOOL notificationPromptShownThisSession;
// Prevent repeated endpoint refresh attempts during a single preload run
@property (atomic, assign) BOOL endpointRefreshAttempted;

@end

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - Implementation
// ─────────────────────────────────────────────────────────────────────────────

@implementation PreloadViewController

// ── Lifecycle ──────────────────────────────────────────────────────────────────

- (void)viewDidLoad
{
    [super viewDidLoad];
    [self pl_setupBackground];
    [self pl_setupLogoAndSpinner];
}

- (void)viewDidLayoutSubviews
{
    [super viewDidLayoutSubviews];
    _backgroundImageView.frame = self.view.bounds;
}

- (void)viewDidAppear:(BOOL)animated
{
    [super viewDidAppear:animated];
    [self startChecks];
}

// ── Public ─────────────────────────────────────────────────────────────────────

- (void)startChecks
{
    self.attributionData = nil;
    [_spinner startAnimating];

    // ── Быстрый путь: режим запуска уже определён при предыдущем запуске ──
    NSString *savedMode = [[NSUserDefaults standardUserDefaults] stringForKey:@"PLLaunchMode"];

    if ([savedMode isEqualToString:@"unity"]) {
        NSLog(@"[PreloadVC] Saved launch mode: Unity — launching directly");
        dispatch_async(dispatch_get_main_queue(), ^{
            [self->_spinner stopAnimating];
            if (self.onComplete) self.onComplete();
        });
        return;
    }

    // webview или первый запуск — всегда пробуем получить свежий URL через полную цепочку.
    // Сохранённый URL используется только как fallback внутри цепочки при ошибках.
    if (savedMode) {
        NSLog(@"[PreloadVC] Saved launch mode: %@ — running full chain to get fresh URL", savedMode);
    }

    // Полная цепочка
    [self pl_updateStatus:@"Starting…" detail:nil progress:0.0];
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        [self pl_step1_checkNetwork];
    });
}

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - UI Setup
// ─────────────────────────────────────────────────────────────────────────────

- (void)pl_setupBackground
{
    _backgroundImageView = [[UIImageView alloc] initWithImage:[UIImage imageNamed:@"LaunchBackground"]];
    _backgroundImageView.frame = self.view.bounds;
    _backgroundImageView.contentMode = UIViewContentModeScaleAspectFill;
    _backgroundImageView.clipsToBounds = YES;
    [self.view insertSubview:_backgroundImageView atIndex:0];
}

- (void)pl_setupLogoAndSpinner
{
    UIView *v = self.view;

    // Логотип
    _logoImageView = [[UIImageView alloc] initWithImage:[UIImage imageNamed:@"AppLogo"]];
    _logoImageView.contentMode = UIViewContentModeScaleAspectFit;
    _logoImageView.translatesAutoresizingMaskIntoConstraints = NO;
    [v addSubview:_logoImageView];

    // Спиннер
    _spinner = [[UIActivityIndicatorView alloc]
                initWithActivityIndicatorStyle:UIActivityIndicatorViewStyleLarge];
    _spinner.color = [UIColor whiteColor];
    _spinner.translatesAutoresizingMaskIntoConstraints = NO;
    [v addSubview:_spinner];
    [_spinner startAnimating];

    [NSLayoutConstraint activateConstraints:@[
        // Логотип — по центру экрана
        [_logoImageView.centerXAnchor constraintEqualToAnchor:v.centerXAnchor],
        [_logoImageView.centerYAnchor constraintEqualToAnchor:v.centerYAnchor constant:-60],
        [_logoImageView.widthAnchor   constraintEqualToAnchor:v.widthAnchor multiplier:0.55],
        [_logoImageView.heightAnchor  constraintEqualToAnchor:_logoImageView.widthAnchor],

        // Спиннер — ниже логотипа
        [_spinner.centerXAnchor constraintEqualToAnchor:v.centerXAnchor],
        [_spinner.topAnchor     constraintEqualToAnchor:_logoImageView.bottomAnchor constant:36],
    ]];
}

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - Этапы загрузки
// ─────────────────────────────────────────────────────────────────────────────
//
//   ┌─ Step 1 ──── Проверка сети              (0.00 → 0.15)
//   ├─ Step 2 ──── Инициализация Firebase      (0.15 → 0.40)
//   ├─ Step 3 ──── AppsFlyerr init + GCD wait  (0.40 → 0.70)
//   └─ Step 4 ──── Запрос к эндпоинту          (0.70 → 1.00)
//                   → onComplete  (Unity)
//                   → onOpenURL   (WebView)
//

// ── Step 1 : Сеть ─────────────────────────────────────────────────────────────

- (void)pl_step1_checkNetwork
{
    [self pl_updateStatus:@"Checking connection…"
                   detail:@"Network"
                 progress:0.05];

    NSString *pingTarget = self.config.endpointURL ?: @"https://apple.com";
    NSURL *pingURL = [NSURL URLWithString:pingTarget];
    NSMutableURLRequest *req = [NSMutableURLRequest requestWithURL:pingURL
                                                       cachePolicy:NSURLRequestReloadIgnoringCacheData
                                                   timeoutInterval:5.0];
    req.HTTPMethod = @"HEAD";

    __weak typeof(self) weakSelf = self;
    [[[NSURLSession sharedSession] dataTaskWithRequest:req
                                    completionHandler:^(NSData *d, NSURLResponse *r, NSError *e) {
        __strong typeof(weakSelf) strongSelf = weakSelf;
        if (!strongSelf) return;

        if (e == nil) {
            [strongSelf pl_updateStatus:@"Connection OK" detail:nil progress:0.15];
            dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
                [strongSelf pl_step2_initFirebase];
            });
            return;
        }

        NSLog(@"[PreloadVC] Network check to %@ failed: %@", pingTarget, e);

        // If the configured endpoint is down (or blocked by ATS), try a known reliable host
        // before showing the "No Internet" UI. This avoids false negatives when only the
        // endpoint is unreachable.
        if (![pingTarget.lowercaseString containsString:@"apple.com"]) {
            NSURL *fallbackURL = [NSURL URLWithString:@"https://apple.com"];
            NSMutableURLRequest *fallbackReq = [NSMutableURLRequest requestWithURL:fallbackURL
                                                                       cachePolicy:NSURLRequestReloadIgnoringCacheData
                                                                   timeoutInterval:5.0];
            fallbackReq.HTTPMethod = @"HEAD";

            [[[NSURLSession sharedSession] dataTaskWithRequest:fallbackReq
                                            completionHandler:^(NSData *d2, NSURLResponse *r2, NSError *e2) {
                __strong typeof(weakSelf) strongSelf2 = weakSelf;
                if (!strongSelf2) return;
                if (e2 == nil) {
                    NSLog(@"[PreloadVC] Fallback network check OK (apple.com)");
                    [strongSelf2 pl_updateStatus:@"Connection OK" detail:@"Endpoint unreachable" progress:0.15];
                    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
                        [strongSelf2 pl_step2_initFirebase];
                    });
                } else {
                    NSLog(@"[PreloadVC] Fallback network check failed: %@", e2);
                    [strongSelf2 pl_showNoInternetRetry];
                }
            }] resume];
        } else {
            [strongSelf pl_showNoInternetRetry];
        }
    }] resume];
}

// ── Step 2 : Firebase ─────────────────────────────────────────────────────────

- (void)pl_step2_initFirebase
{
    [self pl_updateStatus:@"Initializing Firebase…"
                   detail:@"Firebase"
                 progress:0.20];
    // Инициализируем Firebase напрямую — уведомления спрашиваем позже, только при WebView
    [PLServicesWrapper configureFirebase:^(NSError *fbError) {
        if (fbError) {
            NSLog(@"[PreloadVC] Firebase warning (non-fatal): %@", fbError.localizedDescription);
            [self pl_updateStatus:@"Firebase unavailable" detail:fbError.localizedDescription progress:0.40];
        } else {
            [self pl_updateStatus:@"Firebase ready" detail:nil progress:0.40];
        }
        dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
            [self pl_step3_initAppsFlyer];
        });
    }];
}


// Показывает запрос уведомлений если:
//   1. Пользователь ещё не ответил на этот вопрос в ТЕКУЩЕЙ сессии (notificationPromptShownThisSession == NO)
//   2. Статус системы — NotDetermined или Denied (с учётом 3-дневного кулдауна)
// После завершения (в любую сторону) вызывает completion на главном потоке.
- (void)pl_checkAndAskNotificationsIfNeededWithCompletion:(void(^)(void))completion
{
    if (!completion) completion = ^{};

    // Если в эту сессию уже спрашивали — пропускаем
    if (self.notificationPromptShownThisSession) {
        NSLog(@"[PreloadVC] Notification prompt already shown this session — skipping");
        dispatch_async(dispatch_get_main_queue(), ^{ completion(); });
        return;
    }

    if (@available(iOS 10.0, *)) {
        UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
        [center getNotificationSettingsWithCompletionHandler:^(UNNotificationSettings * _Nonnull settings) {
            BOOL shouldRequest = NO;
            if (settings.authorizationStatus == UNAuthorizationStatusNotDetermined) {
                shouldRequest = YES;
            } else if (settings.authorizationStatus == UNAuthorizationStatusDenied) {
                NSDate *lastDenied = [[NSUserDefaults standardUserDefaults] objectForKey:@"PLLastNotificationDeniedAt"];
                if (!lastDenied) {
                    shouldRequest = YES;
                } else {
                    NSTimeInterval since = [[NSDate date] timeIntervalSinceDate:lastDenied];
                    if (since >= (3 * 24 * 60 * 60)) {
                        shouldRequest = YES; // прошло 3 дня
                    }
                }
            }

            if (!shouldRequest) {
                // Системное разрешение уже есть или кулдаун не истёк — помечаем сессию и продолжаем
                self.notificationPromptShownThisSession = YES;
                dispatch_async(dispatch_get_main_queue(), ^{ completion(); });
                return;
            }

            dispatch_async(dispatch_get_main_queue(), ^{
                if (self.isPresentingNotificationPrompt) {
                    completion();
                    return;
                }
                self.isPresentingNotificationPrompt = YES;

                __weak typeof(self) weakSelf = self;
                NotificationPromptViewController *np = [[NotificationPromptViewController alloc]
                    initWithTitle:@"Enable Notifications"
                    message:@"Would you like to receive important notifications about the app?"
                    backgroundImage:nil
                    allowHandler:^{
                        __strong typeof(weakSelf) strongSelf = weakSelf;
                        if (!strongSelf) return;
                        strongSelf.notificationPromptShownThisSession = YES;
                        [[NSUserDefaults standardUserDefaults] setBool:YES forKey:@"PLAskedForNotifications"];
                        [[NSUserDefaults standardUserDefaults] synchronize];
                        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.35 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
                            UNAuthorizationOptions opts = (UNAuthorizationOptionBadge | UNAuthorizationOptionSound | UNAuthorizationOptionAlert);
                            [center requestAuthorizationWithOptions:opts completionHandler:^(BOOL granted, NSError * _Nullable err) {
                                if (!granted) {
                                    [[NSUserDefaults standardUserDefaults] setObject:[NSDate date] forKey:@"PLLastNotificationDeniedAt"];
                                } else {
                                    [[NSUserDefaults standardUserDefaults] removeObjectForKey:@"PLLastNotificationDeniedAt"];
                                }
                                [[NSUserDefaults standardUserDefaults] synchronize];
                                dispatch_async(dispatch_get_main_queue(), ^{
                                    strongSelf.isPresentingNotificationPrompt = NO;
                                    completion();
                                });
                            }];
                        });
                    }
                    cancelHandler:^{
                        __strong typeof(weakSelf) strongSelf = weakSelf;
                        if (!strongSelf) return;
                        strongSelf.notificationPromptShownThisSession = YES;
                        [[NSUserDefaults standardUserDefaults] setBool:YES forKey:@"PLAskedForNotifications"];
                        [[NSUserDefaults standardUserDefaults] setObject:[NSDate date] forKey:@"PLLastNotificationDeniedAt"];
                        [[NSUserDefaults standardUserDefaults] synchronize];
                        strongSelf.isPresentingNotificationPrompt = NO;
                        completion();
                    }];

                [self presentViewController:np animated:YES completion:nil];
            });
        }];
    } else {
        self.notificationPromptShownThisSession = YES;
        dispatch_async(dispatch_get_main_queue(), ^{ completion(); });
    }
}

// ── Step 3 : AppsFlyer ───────────────────────────────────────────────────────

- (void)pl_step3_initAppsFlyer
{
    [self pl_updateStatus:@"Initializing AppsFlyer…"
                   detail:@"AppsFlyer"
                 progress:0.45];

    NSString *devKey   = self.config.appsDevKey ?: @"";
    NSString *appleId  = self.config.appleAppId ?: @"";
    NSTimeInterval tmo = self.config ? self.config.appsflyerTimeout : 15.0;

    // PLServicesWrapper — чистый ObjC, без проблем с C++ модулями
    [PLServicesWrapper startAppsFlyerWithDevKey:devKey
                                     appleAppId:appleId
                               gcdWaitTimeout:tmo
                                     completion:^(NSDictionary *attribution, NSError *error) {
        NSLog(@"[PreloadVC] AppsFlyer attribution: %@", attribution);
        self.attributionData = attribution;
        [self pl_updateStatus:@"AppsFlyer ready" detail:nil progress:0.70];

        dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
            [self pl_step4_requestEndpoint:attribution];
        });
    }];
}

// ── Step 4 : Запрос к эндпоинту ───────────────────────────────────────────────

- (void)pl_step4_requestEndpoint:(nullable NSDictionary *)attribution
{
    NSString *baseURL = self.config.endpointURL;
    if (baseURL.length == 0) {
        NSLog(@"[PreloadVC] endpointURL is empty — proceeding to Unity");
        [self pl_finishWithURL:nil];
        return;
    }

    [self pl_updateStatus:@"Verifying…"
                   detail:@"Server check"
                 progress:0.75];

    // ── Формируем тело запроса ────────────────────────────────────────────────
    NSMutableDictionary *body = [NSMutableDictionary dictionary];

    // Данные устройства
    body[@"bundle_id"]   = [[NSBundle mainBundle] bundleIdentifier] ?: @"";
    body[@"app_version"] = [[[NSBundle mainBundle] infoDictionary]
                            objectForKey:@"CFBundleShortVersionString"] ?: @"";
    body[@"platform"]    = @"ios";
    body[@"idfa"]        = [self pl_idfaString];

    // Данные атрибуции AppsFlyerr
    // Передаём данные конверсии AppsFlyer без изменений, если они есть.
    // Приоритет: сначала сохранённые в PLServicesWrapper (persisted), затем текущие attribution.
    NSDictionary *storedAF = [PLServicesWrapper storedAppsFlyerConversionData];
    if (storedAF && [storedAF isKindOfClass:[NSDictionary class]] && storedAF.count) {
        body[@"appsflyer"] = storedAF;
    } else if (attribution && [attribution isKindOfClass:[NSDictionary class]] && attribution.count) {
        body[@"appsflyer"] = attribution;
    }

    // Дополнительные обязательные поля
    body[@"os"] = @"iOS";
    // store_id берём из конфига (apple App Store id)
    body[@"store_id"] = self.config.appleAppId ?: @"";

    // Firebase fields: project id и push token (если доступны)
    NSString *firebaseProject = [PLServicesWrapper firebaseProjectId];
    if (firebaseProject && firebaseProject.length) {
        body[@"firebase_project_id"] = firebaseProject;
    }
    NSString *pushToken = [PLServicesWrapper firebasePushToken];
    if (pushToken && pushToken.length) {
        body[@"push_token"] = pushToken;
    }

    // ── HTTP запрос ───────────────────────────────────────────────────────────
    NSURL *url = [NSURL URLWithString:[baseURL stringByAppendingString:@"/config.php"]];
    NSMutableURLRequest *req = [NSMutableURLRequest requestWithURL:url];
    req.HTTPMethod = @"POST";
    [req setValue:@"application/json" forHTTPHeaderField:@"Content-Type"];
    [req setValue:@"application/json" forHTTPHeaderField:@"Accept"];

    NSTimeInterval timeout = self.config ? self.config.endpointTimeout : 10.0;
    req.timeoutInterval = timeout;

    NSError *jsonErr = nil;
    NSData  *jsonData = [NSJSONSerialization dataWithJSONObject:body
                                                        options:0
                                                          error:&jsonErr];
    if (jsonErr || !jsonData) {
        NSLog(@"[PreloadVC] JSON serialization error: %@", jsonErr);
        [self pl_finishWithURL:nil];
        return;
    }
    req.HTTPBody = jsonData;

    NSURLSessionConfiguration *cfg = [NSURLSessionConfiguration defaultSessionConfiguration];
    cfg.timeoutIntervalForRequest  = timeout;
    cfg.timeoutIntervalForResource = timeout + 5;
    NSURLSession *session = [NSURLSession sessionWithConfiguration:cfg];

    [[session dataTaskWithRequest:req
                completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {

        if (error) {
            NSLog(@"[PreloadVC] Endpoint request error: %@", error);
            // Сетевая ошибка — показываем экран отсутствия интернета
            [self pl_showNoInternetRetry];
            return;
        }

        NSHTTPURLResponse *http = (NSHTTPURLResponse *)response;
        NSLog(@"[PreloadVC] Endpoint status: %ld", (long)http.statusCode);

        [self pl_updateStatus:@"Processing response…" detail:nil progress:0.90];

        // ── Разбираем ответ ───────────────────────────────────────────────────
        NSURL *redirectURL = nil;

        if (data.length) {
            NSError *parseErr = nil;
            id json = [NSJSONSerialization JSONObjectWithData:data
                                                     options:0
                                                       error:&parseErr];
            if (!parseErr && [json isKindOfClass:[NSDictionary class]]) {
                NSDictionary *dict = (NSDictionary *)json;


                // Новый формат — при ok == true берём url
                id okFlag = dict[@"ok"];
                NSString *urlString = nil;
                if (okFlag) {
                    BOOL ok = NO;
                    if ([okFlag isKindOfClass:[NSNumber class]]) ok = [(NSNumber *)okFlag boolValue];
                    else if ([okFlag isKindOfClass:[NSString class]]) ok = [(NSString *)okFlag boolValue];

                    if (ok) {
                        urlString = dict[@"url"];
                        // логируем + сохраняем expires при наличии
                        id expires = dict[@"expires"];
                        if (expires) {
                            NSLog(@"[PreloadVC] Endpoint expires: %@", expires);
                            // Normalize expires into a unix timestamp (seconds since 1970)
                            double expiresTS = 0;
                            if ([expires isKindOfClass:[NSNumber class]]) {
                                expiresTS = [(NSNumber *)expires doubleValue];
                            } else if ([expires isKindOfClass:[NSString class]]) {
                                // Try ISO8601 first
                                if (@available(iOS 10.0, *)) {
                                    NSISO8601DateFormatter *fmt = [NSISO8601DateFormatter new];
                                    NSDate *d = [fmt dateFromString:(NSString *)expires];
                                    if (d) expiresTS = [d timeIntervalSince1970];
                                }
                                if (expiresTS == 0) {
                                    // Fallback: parse as number string
                                    expiresTS = [(NSString *)expires doubleValue];
                                }
                            }
                            if (expiresTS > 0) {
                                [[NSUserDefaults standardUserDefaults] setDouble:expiresTS forKey:@"PLLastEndpointExpires"];
                                [[NSUserDefaults standardUserDefaults] synchronize];
                                // Reset per-run refresh flag when we get a fresh expires value
                                self.endpointRefreshAttempted = NO;
                            }
                        }
                    }
                }

                if (urlString.length) {
                    redirectURL = [NSURL URLWithString:urlString];
                    // Persist last endpoint URL so WebView can reuse it on next launch
                    if (redirectURL) {
                        [[NSUserDefaults standardUserDefaults] setObject:redirectURL.absoluteString forKey:@"PLLastEndpointURLString"];
                        [[NSUserDefaults standardUserDefaults] synchronize];
                    }
                }
            } else {
                NSLog(@"[PreloadVC] Endpoint parse error: %@", parseErr);
            }
        }

        [self pl_finishWithURL:redirectURL];

    }] resume];
}

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - Финал
// ─────────────────────────────────────────────────────────────────────────────

/// `url == nil`  → запускаем Unity (onComplete) — уведомления НЕ запрашиваем
/// `url != nil`  → показываем WebView (onOpenURL) — сначала запрашиваем уведомления (если не спрашивали в эту сессию)
- (void)pl_finishWithURL:(nullable NSURL *)url
{
    [self pl_updateStatus:@"Done!" detail:nil progress:1.00];

    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.3 * NSEC_PER_SEC)),
                   dispatch_get_main_queue(), ^{
        [self->_spinner stopAnimating];

        // Для WebView-пути: если config.php не вернул URL — используем последний сохранённый
        NSURL *useURL = url;
        if (!useURL) {
            NSString *stored = [[NSUserDefaults standardUserDefaults] stringForKey:@"PLLastEndpointURLString"];
            if (stored.length) {
                useURL = [NSURL URLWithString:stored];
            }
        }

        // ── Сохраняем режим запуска при первом определении ──
        NSString *savedMode = [[NSUserDefaults standardUserDefaults] stringForKey:@"PLLaunchMode"];
        if (!savedMode) {
            NSString *mode = useURL ? @"webview" : @"unity";
            [[NSUserDefaults standardUserDefaults] setObject:mode forKey:@"PLLaunchMode"];
            [[NSUserDefaults standardUserDefaults] synchronize];
            NSLog(@"[PreloadVC] Launch mode saved: %@", mode);
        }

        if (useURL) {
            // ── WebView path: сначала спрашиваем разрешение на уведомления, затем открываем ──
            NSLog(@"[PreloadVC] WebView path — checking notification permission before opening URL");
            [self pl_checkAndAskNotificationsIfNeededWithCompletion:^{
                NSLog(@"[PreloadVC] → opening URL: %@", useURL);
                dispatch_async(dispatch_get_main_queue(), ^{
                    if (self.onOpenURL) {
                        self.onOpenURL(useURL);
                    } else {
                        [[UIApplication sharedApplication] openURL:useURL
                                                           options:@{}
                                                 completionHandler:nil];
                    }
                });
            }];
        } else {
            // ── Unity path: уведомления не запрашиваем ──
            NSLog(@"[PreloadVC] → proceeding to Unity (no notification prompt)");
            if (self.onComplete) self.onComplete();
        }
    });
}

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - Helpers
// ─────────────────────────────────────────────────────────────────────────────

- (void)pl_updateStatus:(NSString *)text
                 detail:(nullable NSString *)detail
               progress:(float)progress
{
    // Визуальные индикаторы статуса/прогресса убраны — только спиннер остаётся
}

- (void)pl_showNoInternetRetry
{
    // Если режим уже определён как webview и есть сохранённый URL —
    // используем его как fallback вместо показа диалога «Нет интернета».
    NSString *savedMode = [[NSUserDefaults standardUserDefaults] stringForKey:@"PLLaunchMode"];
    if ([savedMode isEqualToString:@"webview"]) {
        NSString *stored = [[NSUserDefaults standardUserDefaults] stringForKey:@"PLLastEndpointURLString"];
        NSURL *storedURL = stored.length ? [NSURL URLWithString:stored] : nil;
        if (storedURL) {
            NSLog(@"[PreloadVC] No internet — using stored WebView URL as fallback: %@", storedURL);
            [self pl_checkAndAskNotificationsIfNeededWithCompletion:^{
                dispatch_async(dispatch_get_main_queue(), ^{
                    [self->_spinner stopAnimating];
                    if (self.onOpenURL) {
                        self.onOpenURL(storedURL);
                    } else {
                        [[UIApplication sharedApplication] openURL:storedURL options:@{} completionHandler:nil];
                    }
                });
            }];
            return;
        }
    }

    dispatch_async(dispatch_get_main_queue(), ^{
        [self pl_updateStatus:@"No connection" detail:nil progress:0.0];

        UIAlertController *alert =
            [UIAlertController alertControllerWithTitle:@"No Internet Connection"
                                                message:@"Please check your network settings and try again."
                                         preferredStyle:UIAlertControllerStyleAlert];
        [alert addAction:
            [UIAlertAction actionWithTitle:@"Retry"
                                     style:UIAlertActionStyleDefault
                                   handler:^(UIAlertAction *a) {
                [self startChecks];
            }]];
        [self presentViewController:alert animated:YES completion:nil];
    });
}

/// Возвращает IDFA если доступен, иначе пустую строку.
/// Для iOS 14+ требует ATTrackingManager (раскомментируйте import).
- (NSString *)pl_idfaString
{
    // Раскомментируйте если подключён ATTrackingManager:
    //
    // #import <AppTrackingTransparency/AppTrackingTransparency.h>
    // #import <AdSupport/AdSupport.h>
    // if (@available(iOS 14, *)) {
    //     if ([ATTrackingManager trackingAuthorizationStatus]
    //             == ATTrackingManagerAuthorizationStatusAuthorized) {
    //         return [[[ASIdentifierManager sharedManager] advertisingIdentifier]
    //                 UUIDString];
    //     }
    // }
    return @"";
}

// ── Status bar ────────────────────────────────────────────────────────────────
- (UIStatusBarStyle)preferredStatusBarStyle { return UIStatusBarStyleLightContent; }

@end
