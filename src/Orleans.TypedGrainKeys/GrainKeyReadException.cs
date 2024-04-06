namespace Orleans;

public class GrainKeyReadException : Exception
{
    public GrainKeyReadException(string message) : base(message)
    {
    }
}