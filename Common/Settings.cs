namespace Common
{
    public static class Settings
    {
        // TCP broker
        public const int BROKER_PORT = 9000;
        public const string BROKER_IP = "127.0.0.1";

        // UDP multicast (use a site-local group that behaves better on Windows)
        public const string MULTICAST_GROUP = "239.255.0.1";
        public const int MULTICAST_PORT = 9001;

        // Multicast config for local testing
        public const bool UDP_LOOPBACK = true; // see your own sends on same host
        public const int MULTICAST_TTL = 1;   // stay on local network

        // Persistence
        public const string PERSISTENCE_FILE = "broker_messages.jsonl";
    }
}
