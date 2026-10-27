using UnityEditor;
using UnityEngine;

// Recorded music (Resources/Audio/Music) streams from disk, compressed, instead of being unpacked
// into memory: a minute of stereo would otherwise take ~9 MB of RAM on the phone.
public class MusicImport : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.Contains("/Resources/Audio/Music/")) return;
        var importer = (AudioImporter)assetImporter;
        importer.loadInBackground = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        settings.preloadAudioData = false;
        importer.defaultSampleSettings = settings;
    }
}
