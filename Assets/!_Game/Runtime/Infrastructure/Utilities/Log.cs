namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    public static class Log
    {
        public static readonly TagLog Default = new(string.Empty);
        public static readonly TagLog Editor = new("EDITOR");
        public static readonly TagLog Loading = new("LOADING");
        public static readonly TagLog Battle = new("BATTLE");
        public static readonly TagLog World = new("WORLD");
        public static readonly TagLog City = new("CITY");
        public static readonly TagLog Boot = new("BOOT");
    }
}