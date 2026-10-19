namespace GameLibraryApi.Exceptions;

public sealed class IgdbConfigurationException(string message) : Exception(message);

public sealed class IgdbApiException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class LibraryEntryAlreadyExistsException()
    : Exception("This game is already in the library.");
