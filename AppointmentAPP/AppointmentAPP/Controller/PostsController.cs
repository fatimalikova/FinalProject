using AppointmentAPP.Dtos.PostDtos;
using AppointmentAPP.Extensions;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController(
        IPostService postService,
        IValidator<CreatePostDto> createPostValidator,
        IValidator<CreateCommentDto> createCommentValidator
    ) : BaseController
    {
        [Authorize(Roles = "Provider")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
        {
            var validation = createPostValidator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await postService.CreateAsync(User.GetUserId(), dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Provider")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await postService.DeleteAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult("Post deleted."));
        }

        // Anonim də baxa bilsin, amma login olubsa "isLiked" düzgün gəlsin deyə user-i opsional oxuyuruq
        [AllowAnonymous]
        [HttpGet("provider/{providerId:guid}")]
        public async Task<IActionResult> GetByProvider(Guid providerId)
        {
            Guid? currentUserId = null;
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (idClaim is not null && Guid.TryParse(idClaim, out var parsed))
                currentUserId = parsed;

            var result = await postService.GetByProviderAsync(providerId, currentUserId);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize]
        [HttpPost("{id:guid}/like")]
        public async Task<IActionResult> Like(Guid id)
        {
            await postService.LikeAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult("Post liked."));
        }

        [Authorize]
        [HttpDelete("{id:guid}/like")]
        public async Task<IActionResult> Unlike(Guid id)
        {
            await postService.UnlikeAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult("Like removed."));
        }

        [Authorize]
        [HttpPost("{id:guid}/comments")]
        public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateCommentDto dto)
        {
            var validation = createCommentValidator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await postService.AddCommentAsync(User.GetUserId(), id, dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/comments")]
        public async Task<IActionResult> GetComments(Guid id)
        {
            var result = await postService.GetCommentsAsync(id);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize]
        [HttpDelete("comments/{commentId:guid}")]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            await postService.DeleteCommentAsync(User.GetUserId(), commentId);
            return Ok(ResponseModelHelper.SuccessResult("Comment deleted."));
        }
    }
}
