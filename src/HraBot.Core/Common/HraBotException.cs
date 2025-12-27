namespace HraBot.Core;

public class HraBotException(string message, Exception? innerException = null)
    : Exception(message, innerException) { }
