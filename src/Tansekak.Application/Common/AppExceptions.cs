namespace Tansekak.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message) : base(message)
    {
    }
}
