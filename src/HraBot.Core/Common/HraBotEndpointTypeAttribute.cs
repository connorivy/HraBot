namespace HraBot.Core.Features.Chat;

internal class HraBotEndpointTypeAttribute(string http) : Attribute
{
    public string Http => http;
}
