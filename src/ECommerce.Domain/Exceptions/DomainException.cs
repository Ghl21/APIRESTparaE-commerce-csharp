namespace ECommerce.Domain.Exceptions;

/// <summary>Excepción base para errores controlados de negocio.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}

/// <summary>El recurso solicitado no existe.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entity, object key)
        : base($"No se encontró {entity} con identificador '{key}'.")
    {
    }
}

/// <summary>Se violó una regla de negocio (datos válidos pero operación no permitida).</summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

/// <summary>Conflicto con el estado actual del recurso (duplicados, concurrencia).</summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>Credenciales inválidas o token no autorizado.</summary>
public class AuthenticationException : DomainException
{
    public AuthenticationException(string message) : base(message)
    {
    }
}

/// <summary>El usuario autenticado no tiene permiso sobre el recurso.</summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
