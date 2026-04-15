using System.Data;

namespace ScholarFlow.Domain.Interfaces;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
