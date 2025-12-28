namespace HraBot.Core.Features.Chat;

internal class HraBotRouteAttribute(string route) : Attribute
{
    public string Route => route;
}
