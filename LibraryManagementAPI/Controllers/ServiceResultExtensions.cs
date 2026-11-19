using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers;

internal static class ServiceResultExtensions
{
    /// <summary>Maps a failed <see cref="ServiceResult{T}"/> to the matching HTTP status with an ApiResponse body.</summary>
    public static ActionResult ToErrorResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        var body = ApiResponse<T>.FailureResponse(result.Message);
        return result.Error switch
        {
            ServiceError.NotFound => controller.NotFound(body),
            ServiceError.Conflict => controller.Conflict(body),
            ServiceError.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, body),
            _ => controller.BadRequest(body)
        };
    }
}
