using Common;

namespace Broker
{
    public interface IMessageStore
    {
        void Append(Payload payload);  // persistă
        IEnumerable<Payload> ReadAll(); // replay la pornire
    }
}
