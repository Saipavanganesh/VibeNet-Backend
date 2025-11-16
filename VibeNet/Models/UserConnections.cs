using Microsoft.AspNetCore.Connections;

namespace VibeNet.Models
{
    public class UserConnections
    {
        public string id { get; set; } 
        public List<ConnectionItem> connections { get; set; }
        public DateTime lastUpdated { get; set; }
    }
    public class ConnectionItem
    {
        public string userId { get; set; }  
        public DateTime connectionStartedDate { get; set; }
        public string status { get; set; }  
    }
}
