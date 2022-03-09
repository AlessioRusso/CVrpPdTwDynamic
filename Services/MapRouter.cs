using CVrpPdTwDynamic.Models;

namespace CVrpPdTwDynamic.Services;
public interface IMapRouter
{
    long GetDistance(Rider op, IRoutableLocation from, IRoutableLocation to);
    long GetDuration(Rider op, IRoutableLocation from, IRoutableLocation to);
}

public class MyMapRouter : IMapRouter
{
    public long GetDistance(Rider op, IRoutableLocation from, IRoutableLocation to)
    {
        var dlat = from.Latitude - (double)to.Latitude;
        var dlon = from.Longitude - (double)to.Longitude;
        return (long)Math.Sqrt(dlat * dlat + dlon * dlon);
    }

    public long GetDuration(Rider op, IRoutableLocation from, IRoutableLocation to)
    {
        return GetDistance(op, from, to) / op.Vehicle;
    }
}