using System;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using HraBot.Api.Features.Json;

[assembly: LambdaSerializer(
    typeof(SourceGeneratorLambdaJsonSerializer<HraBotJsonSerializerContext>)
)]
