#import <ReplayKit/ReplayKit.h>
#import <UIKit/UIKit.h>

extern UIViewController *UnityGetGLViewController(void);

extern "C" bool SignalHuntReplay_IsAvailable(void)
{
    if (@available(iOS 10.0, *)) {
        return [RPScreenRecorder sharedRecorder].available;
    }
    return false;
}

extern "C" void SignalHuntReplay_Start(void)
{
    if (@available(iOS 10.0, *)) {
        RPScreenRecorder *recorder = [RPScreenRecorder sharedRecorder];
        if (!recorder.available || recorder.recording) { return; }
        recorder.microphoneEnabled = NO;
        [recorder startRecordingWithHandler:^(NSError *error) {
            if (error) {
                NSLog(@"Signal Hunt ReplayKit start error: %@", error.localizedDescription);
            }
        }];
    }
}

extern "C" void SignalHuntReplay_StopAndPresent(void)
{
    if (@available(iOS 10.0, *)) {
        RPScreenRecorder *recorder = [RPScreenRecorder sharedRecorder];
        if (!recorder.recording) { return; }
        [recorder stopRecordingWithHandler:^(RPPreviewViewController *preview, NSError *error) {
            if (error) {
                NSLog(@"Signal Hunt ReplayKit stop error: %@", error.localizedDescription);
                return;
            }
            if (preview) {
                dispatch_async(dispatch_get_main_queue(), ^{
                    [UnityGetGLViewController() presentViewController:preview animated:YES completion:nil];
                });
            }
        }];
    }
}
