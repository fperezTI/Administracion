namespace AssetManagement.Application.Common.Exceptions;

/// <summary>Raised by AuthorizationBehavior when the caller lacks a required permission or is unauthenticated.</summary>
public sealed class ForbiddenAccessException(string message) : Exception(message);
