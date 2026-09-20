namespace AssetManagement.Application.Common.Exceptions;

/// <summary>Raised when a request conflicts with current state (e.g. a policy limit, a duplicate, a cycle).</summary>
public sealed class ConflictException(string message) : Exception(message);
