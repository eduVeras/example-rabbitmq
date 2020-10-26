using System.Collections.Generic;
using System.Text;

namespace Publisher.Model
{
    public class Message
    {
        public Message()
        {
            Header = null;
        }

        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public string Content { get; set; }
        public Dictionary<string, object> Header { get; set; }

        public byte[] GetContentBeforeSend()
        {
            return Encoding.UTF8.GetBytes(Content);
        }
    }
}
