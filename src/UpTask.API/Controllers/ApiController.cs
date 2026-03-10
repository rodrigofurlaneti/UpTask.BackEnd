using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Exceptions;
using System.Diagnostics;

namespace UpTask.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiController(ISender mediator, ICurrentUserService currentUser) : ControllerBase
{
    protected readonly ISender _mediator = mediator;

    /// <summary>
    /// Recupera o ID do usuário logado com rastreio de erro para depuração.
    /// </summary>
    protected Guid CurrentUserId
    {
        get
        {
            var userId = currentUser.UserId;

            if (userId == null)
            {
                // Rastreio no Console do Debug para identificar por que o 401 está ocorrendo
                Debug.WriteLine("=== AUTH DEBUG ===");
                Debug.WriteLine($"User Identity IsAuthenticated: {User.Identity?.IsAuthenticated}");
                Debug.WriteLine($"Claims Count: {User.Claims.Count()}");

                foreach (var claim in User.Claims)
                {
                    Debug.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                }

                throw new UnauthorizedException("User not authenticated or Token claims mismatch.");
            }

            return userId.Value;
        }
    }

    /// <summary>
    /// Wrapper para as chamadas do Mediator com captura de erro centralizada.
    /// </summary>
    protected async Task<IActionResult> ExecuteCommand<T>(IRequest<T> command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log do erro real que está acontecendo no Handler ou no Banco
            Debug.WriteLine("=== COMMAND ERROR ===");
            Debug.WriteLine($"Error Type: {ex.GetType().Name}");
            Debug.WriteLine($"Message: {ex.Message}");

            if (ex.InnerException != null)
                Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");

            // Repassa a exceção para que o seu GlobalExceptionHandler (Middleware) 
            // formate a resposta JSON correta para o Front-end.
            throw;
        }
    }
}