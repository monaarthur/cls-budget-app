using CLS.Budget.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CLS.Budget.UnitTests.Controllers;

internal static class ControllerTestHelper
{
    public static void AttachControllerContext(ControllerBase controller, string controllerName)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData { Values = { ["controller"] = controllerName } },
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor
            {
                ControllerName = controllerName
            }
        };
    }

    public static ApiResponse<T> GetEnvelope<T>(this IActionResult result)
    {
        var value = result switch
        {
            OkObjectResult ok => ok.Value,
            NotFoundObjectResult notFound => notFound.Value,
            BadRequestObjectResult badRequest => badRequest.Value,
            CreatedAtActionResult created => created.Value,
            _ => throw new InvalidOperationException($"Unexpected result type: {result.GetType().Name}")
        };

        return value.Should().BeOfType<ApiResponse<T>>().Subject;
    }
}
