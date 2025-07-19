using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ProbDetail.Mvc.Controllers;

public class BookDto
{
    [Required(ErrorMessage = "Title is required.")]
    public required string Title { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Year must be a positive number.")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Author is required.")]
    [MinLength(2, ErrorMessage = "Author must be at least 2 characters long.")]
    [RegularExpression(@"^[A-Z].*", ErrorMessage = "Author must start with a capital letter.")]
    public required string Author { get; set; }
}

[Route("api/books")]
[ApiController]
public class BooksController : ControllerBase
{
    private readonly ILogger<BooksController> _logger;

    public BooksController(ILogger<BooksController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{id}")]
    public Results<ValidationProblem, Ok<object>> GetBook(int id)
    {
        if (id < 0)
        {
            return TypedResults.ValidationProblem(errors: [new("id", ["ID must be a non-negative integer."])]);
        }

        return TypedResults.Ok(new { Message = $"Book with ID: {id}" } as object);
    }

    [HttpGet("forbidden")]
    public IActionResult GetForbidddenProblem()
    {
        return Problem(
            statusCode: StatusCodes.Status403Forbidden,
            detail: "Insufficient permissions to access this endpoint.");
    }

    [HttpGet("not-found")]
    public IActionResult GetCustomException()
    {
        throw new Utils.ProblemDetailsException(new ProblemDetails
        {
            Title = "Not found",
            Detail = "The requested book was not found",
            Status = StatusCodes.Status404NotFound,
            Type = "urn:acme-corp:errors:not-found"
        });
    }

    [HttpPost]
    public IActionResult CreateBook([FromBody] BookDto book)
    {
        _logger.LogInformation($"Creating book with title: {book.Title}");
        return CreatedAtAction(nameof(GetBook), new { id = 1 }, book);
    }
}
