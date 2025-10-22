using Common;

namespace Broker
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Broker starting...");

            // 1) PERSISTENT STORE
            var store = new FileMessageStore(Settings.PERSISTENCE_FILE);

            // 2) Replay din persistentă (opțional, demonstrează durability)
            foreach (var p in store.ReadAll())
                PayloadStorage.Enqueue(p);

            // 3) TCP accept
            var server = new BrokerSocket(store);
            server.Start(Settings.BROKER_IP, Settings.BROKER_PORT);

            // 4) Worker (TCP unicast + UDP multicast)
            var worker = new MessageWorker();
            var t = Task.Factory.StartNew(worker.Run, TaskCreationOptions.LongRunning);

            Console.WriteLine($"Broker on TCP {Settings.BROKER_IP}:{Settings.BROKER_PORT}, " +
                              $"UDP multicast {Settings.MULTICAST_GROUP}:{Settings.MULTICAST_PORT}");
            Console.WriteLine("Press ENTER to stop.");
            Console.ReadLine();

            PayloadStorage.Complete();
            t.Wait(1000);
        }
    }
}
