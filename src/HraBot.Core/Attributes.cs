using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using HraBot.Api.Features.Json;

[assembly: LambdaSerializer(typeof(CustomLambdaSerializer))]
[assembly: LambdaGlobalProperties(GenerateMain = false, Runtime = "dotnet10")]
