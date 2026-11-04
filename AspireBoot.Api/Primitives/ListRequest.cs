using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Primitives;

public record ListRequest(
    [FromQuery]
    [property: Range(1, int.MaxValue)]
    int Page = 1,
    [FromQuery] [property: Range(1, 100)] int Size = 10);
