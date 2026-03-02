using System;
using System.Collections.Generic;

namespace TopSpeed.Data
{
    public static partial class TrackCatalog
    {
        public static IReadOnlyDictionary<string, TrackData> BuiltIn => BuiltInMap.Value;

        private static readonly Lazy<IReadOnlyDictionary<string, TrackData>> BuiltInMap =
            new Lazy<IReadOnlyDictionary<string, TrackData>>(() => new Dictionary<string, TrackData>(StringComparer.Ordinal)
            {
                ["america"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrAmerica!),
                ["austria"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrAustria!),
                ["belgium"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrBelgium!),
                ["brazil"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrBrazil!),
                ["china"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrChina!),
                ["england"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrEngland!),
                ["finland"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrFinland!),
                ["france"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrFrance!),
                ["germany"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrGermany!),
                ["ireland"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrIreland!),
                ["italy"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrItaly!),
                ["netherlands"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrNetherlands!),
                ["portugal"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrPortugal!),
                ["russia"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrRussia!),
                ["spain"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrSpain!),
                ["sweden"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrSweden!),
                ["switserland"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrSwitserland!),
                ["advHills"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrAdvHills!),
                ["advCoast"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrAdvCoast!),
                ["advCountry"] = new TrackData(false, TrackWeather.Rain, TrackAmbience.NoAmbience, TrAdvCountry!),
                ["advAirport"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.Airport, TrAirport!),
                ["advDesert"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.Desert, TrDesert!),
                ["advRush"] = new TrackData(false, TrackWeather.Sunny, TrackAmbience.NoAmbience, TrAdvRush!),
                ["advEscape"] = new TrackData(false, TrackWeather.Wind, TrackAmbience.NoAmbience, TrAdvEscape!)
            });
    }
}
