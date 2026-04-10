#import "CustomAppController.h"
#import "PreloadViewController.h"
#import "WebViewController.h"
#import "WebViewConfig.h"
#import "EasyLaunchConfig.h"

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - Private interface
// ─────────────────────────────────────────────────────────────────────────────

@interface CustomAppController ()

/// Временное окно с экраном загрузки
@property (nonatomic, strong, nullable) UIWindow *preloadWindow;

/// Сцена, полученная при первом вызове initUnityWithScene: — сохраняем для
/// передачи в super после завершения проверок
@property (nonatomic, weak, nullable) UIWindowScene *pendingScene;

/// Флаг: preload уже запущен и ждём завершения проверок
@property (nonatomic, assign) BOOL preloadInProgress;

@end

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - Implementation
// ─────────────────────────────────────────────────────────────────────────────

@implementation CustomAppController

/// Перехватываем точку входа Unity.
/// Если движок ещё не инициализировался — сначала показываем preload-экран,
/// а запуск Unity откладываем до завершения всех проверок.
/// Повторные вызовы (возврат из фона после инициализации) пробрасываем в super.
- (void)initUnityWithScene:(UIWindowScene *)scene
{
    // Если Unity уже инициализирован — обычное поведение (return внутри super)
    if (self.engineLoadState >= kUnityEngineLoadStateCoreInitialized)
    {
        [super initUnityWithScene:scene];
        return;
    }

    // Если preload уже запущен (повторный вызов пока идут проверки) — игнорируем
    if (self.preloadInProgress)
        return;

    self.preloadInProgress = YES;
    self.pendingScene = scene;

    [self showPreloadScreenForScene:scene];
}

// ─────────────────────────────────────────────────────────────────────────────
// MARK: - Preload window
// ─────────────────────────────────────────────────────────────────────────────

- (void)showPreloadScreenForScene:(UIWindowScene *)scene
{
    dispatch_async(dispatch_get_main_queue(), ^{
        // Создаём отдельное UIWindow поверх всего
        UIWindow *preloadWindow;
        if (scene != nil) {
            preloadWindow = [[UIWindow alloc] initWithWindowScene:scene];
        } else {
            preloadWindow = [[UIWindow alloc] initWithFrame:UIScreen.mainScreen.bounds];
        }
        // Ensure UI outside presented controllers/webview is black
        preloadWindow.backgroundColor = [UIColor blackColor];
        // Уровень окна: выше стандартного, но ниже системных алертов
        preloadWindow.windowLevel = UIWindowLevelNormal + 10;

        PreloadViewController *vc = [[PreloadViewController alloc] init];

        PreloadConfig *cfg = [PreloadConfig configWithAppsDevKey:EL_APPSFLYER_DEV_KEY
                                                      appleAppId:EL_APPLE_APP_ID
                                                     endpointURL:EL_ENDPOINT_URL];
        vc.config = cfg;

        // По завершении всех проверок — скрываем preload и запускаем Unity
        __weak typeof(self) weakSelf = self;
        vc.onComplete = ^{
            [weakSelf dismissPreloadAndStartUnity];
        };

        // Если сервер вернул URL — открыть во встроенном WebView
        vc.onOpenURL = ^(NSURL *url) {
            dispatch_async(dispatch_get_main_queue(), ^{
                if (url) {
                    WebViewController *wvc = [[WebViewController alloc] initWithURL:url];
                    __weak typeof(self) weakSelf2 = weakSelf;
                    wvc.onClose = ^{
                        [weakSelf2 dismissPreloadAndStartUnity];
                    };
                    // Present the WebViewController directly (no nav bar/header)
                    wvc.modalPresentationStyle = UIModalPresentationFullScreen;
                    if (@available(iOS 13.0, *)) {
                        wvc.modalInPresentation = YES;
                    }
                    [preloadWindow.rootViewController presentViewController:wvc animated:YES completion:nil];
                }
            });
        };

        preloadWindow.rootViewController = vc;
        [preloadWindow makeKeyAndVisible];
        self.preloadWindow = preloadWindow;
    });
}

- (void)dismissPreloadAndStartUnity
{
    // Гарантируем выполнение на главном потоке
    dispatch_async(dispatch_get_main_queue(), ^{
        UIWindow *preloadWindow = self.preloadWindow;

        // Плавное исчезновение preload-экрана
        [UIView animateWithDuration:0.4
                              delay:0.0
                            options:UIViewAnimationOptionCurveEaseIn
                         animations:^{
            preloadWindow.alpha = 0.0;
        }
                         completion:^(BOOL finished) {
            preloadWindow.hidden = YES;
            self.preloadWindow = nil;
            self.preloadInProgress = NO;

            // Теперь инициализируем Unity
            [super initUnityWithScene:self.pendingScene];
        }];
    });
}

@end
