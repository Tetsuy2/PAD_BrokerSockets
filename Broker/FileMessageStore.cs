using System.Collections.Concurrent;
using System.Text;
using Common;
using Newtonsoft.Json;

namespace Broker
{
    public class FileMessageStore : IMessageStore
    {
        private readonly string _path;
        private readonly object _lock = new();

        public FileMessageStore(string path)
        {
            _path = path;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
            if (!File.Exists(_path)) File.WriteAllText(_path, string.Empty);
        }

        public void Append(Payload payload)
        {
            var line = JsonConvert.SerializeObject(payload) + Environment.NewLine;
            lock (_lock)
            {
                File.AppendAllText(_path, line, Encoding.UTF8);
            }
        }

        public IEnumerable<Payload> ReadAll()
        {
            if (!File.Exists(_path)) yield break;
            foreach (var line in File.ReadLines(_path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                Payload? p = null;
                try { p = JsonConvert.DeserializeObject<Payload>(line); }
                catch { /* skip */ }
                if (p != null) yield return p;
            }
        }
    }
}
