namespace Consumer.Model
{
    public class MessagingConfiguration
    {
        public string[] Hostname { get; set; }
        public bool Durable { get; set; }
        public long Ttl { get; set; }
        public short Retries { get; set; }
        public Consuming Consuming { get; set; }
        public Publishing Publishing { get; set; }
        public string ConnectionString { get; set; }
    }

    public class Consuming
    {
        public string Queue { get; set; }
        public string Bindingkey { get; set; }
        public Exchange Exchange { get; set; }
        public Deadletter Deadletter { get; set; }
    }

    public class Publishing
    {
        public string Queue { get; set; }
        public string Routingkey { get; set; }
        public Exchange Exchange { get; set; }
        public Deadletter Deadletter { get; set; }
    }

    public class Deadletter
    {
        public string Queue { get; set; }
        public string Routingkey { get; set; }
        public Exchange Exchange { get; set; }
    }

    public class Exchange
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
