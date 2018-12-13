using Microsoft.AspNetCore.Mvc;

namespace Netptune.Models.Repositories
{
    public class RepoResult<T>
    {

        public string Message { get; }

        public RepoResultStatus Status { get; }

        public T Result { get; }

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
        {
            return new RepoResult<T>(message, RepoResultStatus.NotFound);
        }

        public static RepoResult<T> BadRequest(string message = "BadRequest")
        {
            return new RepoResult<T>(message, RepoResultStatus.BadRequest);
        }

        public static RepoResult<T> Unauthorized(string message = "Unauthorized")
        {
            return new RepoResult<T>(message, RepoResultStatus.Unauthorized);
        }

        public static RepoResult<T> Ok(T result, string message = "Ok")
        {
            return new RepoResult<T>(message, RepoResultStatus.Ok, result);
        }

        public static RepoResult<T> Ok(string message = "Ok")
        {
            return new RepoResult<T>(message, RepoResultStatus.Ok);
        }

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
                    return new OkObjectResult(Message);
            }

            return new StatusCodeResult(500);
        }

    }
}
