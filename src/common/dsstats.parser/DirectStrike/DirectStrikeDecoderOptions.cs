using s2protocol.NET;

namespace Sc2DirectStrike.Parser;

/// <summary>Creates efficient replay-decoder options for the Direct Strike parser.</summary>
public static class DirectStrikeDecoderOptions
{
    public static ReplayDecoderOptions Create()
    {
        return new ReplayDecoderOptions
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            MessageEvents = false,
            TrackerEvents = true,
            GameEvents = true,
            AttributeEvents = false,
        };
    }
}
