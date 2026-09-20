namespace AssetManagement.Domain.SharedKernel;

/// <summary>Raised when an operation would violate a domain invariant or business rule.</summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
