using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Filters;
using SpeedClaim.Api.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SpeedClaim.Tests.Swagger;

[TestFixture]
public class IdempotencyOperationFilterTests
{
    private IdempotencyOperationFilter _filter = null!;

    [SetUp]
    public void Setup()
    {
        _filter = new IdempotencyOperationFilter();
    }

    private class FakeController
    {
        [Idempotent]
        public IActionResult IdempotentEndpoint() => null!;

        public IActionResult NormalEndpoint() => null!;
    }

    private OperationFilterContext CreateContext(string methodName)
    {
        var methodInfo = typeof(FakeController).GetMethod(methodName)!;
        var apiDescription = new ApiDescription();

        var schemaRepo = new SchemaRepository();
        var schemaGen = new Mock<ISchemaGenerator>();

        return new OperationFilterContext(apiDescription, schemaGen.Object, schemaRepo, methodInfo);
    }

    [Test]
    public void IdempotentEndpoint_AddsHeaderParameter()
    {
        var operation = new OpenApiOperation();
        var context = CreateContext(nameof(FakeController.IdempotentEndpoint));

        _filter.Apply(operation, context);

        Assert.That(operation.Parameters, Has.Count.EqualTo(1));
        var param = operation.Parameters.First();
        Assert.That(param.Name, Is.EqualTo("Idempotency-Key"));
        Assert.That(param.In, Is.EqualTo(ParameterLocation.Header));
        Assert.That(param.Required, Is.False);
        Assert.That(param.Schema.Format, Is.EqualTo("uuid"));
    }

    [Test]
    public void IdempotentEndpoint_AddsDescriptionText()
    {
        var operation = new OpenApiOperation();
        var context = CreateContext(nameof(FakeController.IdempotentEndpoint));

        _filter.Apply(operation, context);

        Assert.That(operation.Description, Does.Contain("Idempotent"));
    }

    [Test]
    public void NormalEndpoint_NoHeaderAdded()
    {
        var operation = new OpenApiOperation();
        var context = CreateContext(nameof(FakeController.NormalEndpoint));

        _filter.Apply(operation, context);

        Assert.That(operation.Parameters, Is.Null.Or.Empty);
    }

    [Test]
    public void NormalEndpoint_DescriptionUnchanged()
    {
        var operation = new OpenApiOperation { Description = "Existing description" };
        var context = CreateContext(nameof(FakeController.NormalEndpoint));

        _filter.Apply(operation, context);

        Assert.That(operation.Description, Is.EqualTo("Existing description"));
    }
}
