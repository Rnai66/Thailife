using System;
using System.Threading.Tasks;
using UnityEngine;

// Beach hotels (hotel.lua): your booking and the room tiers.
[RequireComponent(typeof(NakamaConnection))]
public class HotelClient : MonoBehaviour
{
    [Serializable] public class Tier { public string id, name, perks; public int price; }

    [Serializable]
    public class State
    {
        public Tier[] tiers;
        public string tier, room, hotel, done;
        public int ends_in_s;
    }

    [Serializable] private class BookRequest { public string tier, hotel; }

    public static HotelClient Instance { get; private set; }
    public State Current { get; private set; }
    public bool Booked => Current != null && !string.IsNullOrEmpty(Current.tier) && Current.ends_in_s - (Time.realtimeSinceStartup - refreshedAt) > 0;
    public event Action<State> Changed;

    private NakamaConnection connection;
    private float refreshedAt;

    public float SecondsLeft => Current == null ? 0f : Mathf.Max(0f, Current.ends_in_s - (Time.realtimeSinceStartup - refreshedAt));

    private void Awake()
    {
        Instance = this;
        connection = GetComponent<NakamaConnection>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Task<State> RefreshAsync() => Call("hotel_get", "{}");
    public Task<State> BookAsync(string tier, string hotel) => Call("hotel_book", JsonUtility.ToJson(new BookRequest { tier = tier, hotel = hotel }));

    private async Task<State> Call(string rpc, string payload)
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var state = JsonUtility.FromJson<State>(result.Payload);
        Current = state;
        refreshedAt = Time.realtimeSinceStartup;
        Changed?.Invoke(state);
        if (rpc == "hotel_book")
        {
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            LifeClient.Instance?.Refresh(); // breakfast in the bag, suite energy
        }
        return state;
    }
}
