using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Auth.Commands.Logout;

public record LogoutCommand([Required] string RefreshToken) : IRequest;
