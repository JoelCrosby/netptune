using Microsoft.AspNetCore.Mvc;

namespace Netptune.Models.Repositories
{
    public class RepoResult<T>
    {

        public string Message { get; }

        public RepoResultStatus Status { get; }

        public T Result { get; }

        private RepoResult(RepoResultStatus status)
        {
            Status = status;
        }

        private RepoResult(string message, RepoResultStatus status)
        {
            Message = message;
            Status = status;
        }

        private RepoResult(string message, RepoResultStatus status, T result)
        {
            Message = message;
            Status = status;
            Result = result;
        }

        public static RepoResult<T> NotFound(string message = "Not found")
            => new RepoResult<T>(message, RepoResultStatus.NotFound);

        public static RepoResult<T> BadRequest(string message = "Bad Request")
            => new RepoResult<T>(message, RepoResultStatus.BadRequest);

        public static RepoResult<T> Unauthorized(string message = "Unauthorized")
            => new RepoResult<T>(message, RepoResultStatus.Unauthorized);

        public static RepoResult<T> Ok(T result, string message = "Ok")
            => new RepoResult<T>(message, RepoResultStatus.Ok, result);

        public static RepoResult<T> Ok(string message = "Ok")
            => new RepoResult<T>(message, RepoResultStatus.Ok);

        public static RepoResult<T> NoContent()
            => new RepoResult<T>(RepoResultStatus.NoContent);

        public IActionResult ToRestResult()
        {
            switch(Status)
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
