using System;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using HraBot.Api.Features.Json;

[assembly: LambdaSerializer(
    typeof(SourceGeneratorLambdaJsonSerializer<HraBotJsonSerializerContext>)
)]
[assembly: LambdaGlobalProperties(GenerateMain = false, Runtime = "dotnet10")]
