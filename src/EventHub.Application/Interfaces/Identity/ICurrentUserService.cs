namespace EventHub.Application.Interfaces.Identity;

public interface ICurrentUserService
{
    int? UserId { get; }
}
