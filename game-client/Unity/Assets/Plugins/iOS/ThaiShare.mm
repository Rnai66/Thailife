// Native share sheet for the level-up card: Facebook, LINE, Messenger, Instagram, Save Image...
// Reports back to Unity (UnitySendMessage) with "1|<activity type>" when the person finished sharing,
// "0|" when they closed it.
#import <UIKit/UIKit.h>

extern UIViewController* UnityGetGLViewController();
extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);

extern "C" void ThaiShare_Image(const char* path, const char* text, const char* receiver)
{
    NSString* file = [NSString stringWithUTF8String:path];
    NSString* caption = text ? [NSString stringWithUTF8String:text] : @"";
    NSString* target = [NSString stringWithUTF8String:receiver];
    UIImage* image = [UIImage imageWithContentsOfFile:file];
    NSMutableArray* items = [NSMutableArray array];
    if (image != nil) [items addObject:image];
    if (caption.length > 0) [items addObject:caption];
    if (items.count == 0) { UnitySendMessage([target UTF8String], "OnShareDone", "0|"); return; }

    UIActivityViewController* sheet = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
    sheet.completionWithItemsHandler = ^(UIActivityType type, BOOL completed, NSArray* returned, NSError* error) {
        NSString* msg = [NSString stringWithFormat:@"%d|%@", completed ? 1 : 0, type != nil ? type : @""];
        UnitySendMessage([target UTF8String], "OnShareDone", [msg UTF8String]);
    };
    UIViewController* root = UnityGetGLViewController();
    if (sheet.popoverPresentationController != nil) // iPad: a popover from the middle of the screen
    {
        sheet.popoverPresentationController.sourceView = root.view;
        sheet.popoverPresentationController.sourceRect = CGRectMake(root.view.bounds.size.width / 2, root.view.bounds.size.height / 2, 1, 1);
        sheet.popoverPresentationController.permittedArrowDirections = 0;
    }
    [root presentViewController:sheet animated:YES completion:nil];
}
