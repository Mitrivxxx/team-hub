namespace TeamHub.Observability.Middleware;

public interface IExceptionProblemDetailsMapper
{
    bool TryMap(Exception exception, out ExceptionMapping mapping);
}
