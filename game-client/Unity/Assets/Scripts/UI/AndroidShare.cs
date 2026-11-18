using UnityEngine;

// Sharing a picture on Android (the level-up card, the café photo): the PNG goes into the phone's
// gallery (Pictures/ThaiLife9, through MediaStore, so no storage permission and no FileProvider are
// needed on Android 10+), then the system share sheet opens with it and the caption. Android doesn't
// say which app was picked, so the callers count the share once the player comes back to the game
// (OnApplicationFocus). iOS has its own native sheet (Plugins/iOS/ThaiShare.mm).
public static class AndroidShare
{
    // true if the share sheet came up
    public static bool ShareImage(string path, string text, string chooserTitle = "แชร์")
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            byte[] png = System.IO.File.ReadAllBytes(path);
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");

            using var values = new AndroidJavaObject("android.content.ContentValues");
            values.Call("put", "_display_name", "ThaiLife9_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            values.Call("put", "mime_type", "image/png");
            values.Call("put", "relative_path", "Pictures/ThaiLife9");
            using var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media");
            using var collection = media.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
            using var uri = resolver.Call<AndroidJavaObject>("insert", collection, values);
            if (uri == null) return false;
            using (var stream = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
            {
                stream.Call("write", (sbyte[])(System.Array)png);
                stream.Call("flush");
                stream.Call("close");
            }

            using var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND");
            intent.Call<AndroidJavaObject>("setType", "image/png");
            intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri);
            intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text);
            intent.Call<AndroidJavaObject>("addFlags", 1); // FLAG_GRANT_READ_URI_PERMISSION
            using var intentClass = new AndroidJavaClass("android.content.Intent");
            using var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, Lang.T(chooserTitle));
            activity.Call("startActivity", chooser);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AndroidShare] " + e.Message);
            return false;
        }
#else
        return false;
#endif
    }
}
