namespace HraBot.Core.Features.Chat;

internal class HraBotEndpointAttribute(string http, string route) : Attribute
{
    public string Route => route;
    public string Http => http;
}
