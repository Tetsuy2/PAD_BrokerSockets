using Common;

namespace Subscriber
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Subscriber...");
            var sub = new SubscriberSocket();
            sub.Connect(Settings.BROKER_IP, Settings.BROKER_PORT);

            Console.Write("Join UDP multicast? (y/n): ");
            var choice = Console.ReadLine();
            if (choice?.ToLower() == "y") sub.StartMulticast(Settings.MULTICAST_GROUP, Settings.MULTICAST_PORT);

            Console.WriteLine("Enter topics (comma-separated). 'q' to exit.");
            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line?.Trim().ToLower() == "q") break;

                foreach (var t in (line ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    sub.Subscribe(t.ToLower());
            }

            sub.Close();
        }
    }
}
