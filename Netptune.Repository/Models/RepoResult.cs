using Microsoft.AspNetCore.Mvc;

namespace Netptune.Repositories.Models
{
    public sealed class RepoResult<TResult>
    {
        public string Message { get; }

        public RepoResultStatus Status { get; }

        public TResult Result { get; }

        private RepoResult(RepoResultStatus status)
        {
            Status = status;
        }

        private RepoResult(string message, RepoResultStatus status)
        {
            Message = message;
            Status = status;
        }

        private RepoResult(string message, RepoResultStatus status, TResult result)
        {
            Message = message;
            Status = status;
            Result = result;
        }

        public static RepoResult<TResult> NotFound(string message = "Not found")
            => new RepoResult<TResult>(message, RepoResultStatus.NotFound);

        public static RepoResult<TResult> BadRequest(string message = "Bad Request")
            => new RepoResult<TResult>(message, RepoResultStatus.BadRequest);

        public static RepoResult<TResult> Unauthorized(string message = "Unauthorized")
            => new RepoResult<TResult>(message, RepoResultStatus.Unauthorized);

        public static RepoResult<TResult> Ok(TResult result, string message = "Ok")
            => new RepoResult<TResult>(message, RepoResultStatus.Ok, result);

        public static RepoResult<TResult> Ok(string message = "Ok")
            => new RepoResult<TResult>(message, RepoResultStatus.Ok);

        public static RepoResult<TResult> NoContent()
            => new RepoResult<TResult>(RepoResultStatus.NoContent);

        public IActionResult ToRestResult()
        {
            switch (Status)
            {
                case RepoResultStatus.NotFound:
                    return new NotFoundObjectResult(Message);
                case RepoResultStatus.BadRequest:
                    return new BadRequestObjectResult(Message);
                case RepoResultStatus.Unauthorized:
                    return new UnauthorizedObjectResult(Message);
                case RepoResultStatus.Ok:
                    return new OkObjectResult(Result);
                case RepoResultStatus.NoContent:
                    return new NoContentResult();
            }

            return new StatusCodeResult(500);
        }
    }
}
