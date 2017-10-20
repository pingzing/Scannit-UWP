using HslTravelSharp.Core.Models;
using System;

namespace Scannit.Broker
{
    public class SharedFileModel
    {
        public TravelCard MostRecentlySeenCard { get; set; }
        public DateTimeOffset SeenTime { get; set; }
        public bool IsAppInForeground { get; set; }
    }
}
