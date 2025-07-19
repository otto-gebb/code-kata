using Microsoft.AspNetCore.Mvc;

namespace ProbDetail.Utils
{
    /// <summary>
    /// An exception that contains <see cref="ProblemDetails"/> for detailed error reporting to the client.
    /// </summary>
    public class ProblemDetailsException : Exception
    {
        public ProblemDetailsException(ProblemDetails details)
        {
            ProblemDetails = details;
        }

        /// <summary>
        /// The <see cref="ProblemDetails"/> instance that contains error details.
        /// </summary>
        public ProblemDetails ProblemDetails { get; }
    }
}
