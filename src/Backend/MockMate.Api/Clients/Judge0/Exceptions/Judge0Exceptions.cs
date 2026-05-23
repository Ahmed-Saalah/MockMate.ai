namespace MockMate.Api.Clients.Judge0.Exceptions;

public class Judge0Exception : Exception
{
    public Judge0Exception(string message) : base(message) { }
    public Judge0Exception(string message, Exception innerException) : base(message, innerException) { }
}

public class Judge0TooManyRequestsException : Judge0Exception
{
    public Judge0TooManyRequestsException(string message) : base(message) { }
}

public class Judge0ServiceException : Judge0Exception
{
    public Judge0ServiceException(string message) : base(message) { }
    public Judge0ServiceException(string message, Exception innerException) : base(message, innerException) { }
}
