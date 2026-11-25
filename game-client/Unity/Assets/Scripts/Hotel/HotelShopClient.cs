// The lobby of your hotel in the City (cafe.lua hotelshop_*): furnished like the café — the desk, key
// rack, sofas, chandelier — with a name over the door. CafeSite (kind "hotel") shows it, CafeUI changes it.
public class HotelShopClient : CafeClient
{
    public override string Kind => "hotel";
}
