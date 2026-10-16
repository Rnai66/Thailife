using UnityEngine;

// Connection settings for the Nakama server (Heroic Cloud or a local Docker server).
// Create via: Thai Life > Create Nakama Config. The asset is git-ignored.
[CreateAssetMenu(fileName = "NakamaConfig", menuName = "Thai Life/Nakama Config")]
public class NakamaConfig : ScriptableObject
{
    [Tooltip("https for Heroic Cloud, http for a local server")]
    public string scheme = "https";

    [Tooltip("Heroic Cloud: the host shown on your project's page, without https://")]
    public string host = "";

    [Tooltip("443 for Heroic Cloud, 7350 for a local server")]
    public int port = 443;

    [Tooltip("Server key from the Heroic Cloud project settings")]
    public string serverKey = "";

    public bool IsValid => !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(serverKey);
}
