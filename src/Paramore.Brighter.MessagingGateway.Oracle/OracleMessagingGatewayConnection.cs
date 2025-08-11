using System.Text;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public class OracleMessagingGatewayConnection(IAmARelationalDatabaseConfiguration configuration) : IAmGatewayConfiguration
{
    public OracleMessagingGatewayConnection(string connectionString)
        : this(new RelationalDatabaseConfiguration(connectionString))
    {
        
    }
    
    public IAmARelationalDatabaseConfiguration Configuration { get; } = configuration;
    
    public OracleAQAgent? DefaultSender { get; set; }
    
    public Encoding DefaultEncoding { get; set; } = Encoding.UTF8;
}
