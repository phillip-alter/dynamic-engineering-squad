//This file defines a configuration class (also called an Options class).
//Its purpose is to store settings for Google Maps in a strongly-typed way.

namespace InfrastructureApp.Configuration
{
    public class GoogleMapsOptions
    {
        public string ApiKey { get; set; } = "";
    }
}