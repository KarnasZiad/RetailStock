namespace RetailStock.Application.Common;

public sealed class ConcurrencyConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
