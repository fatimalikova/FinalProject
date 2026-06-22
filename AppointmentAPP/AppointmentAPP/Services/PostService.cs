using AppointmentAPP.Data;
using AppointmentAPP.Dtos.PostDtos;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class PostService(AppDbContext db) : IPostService
    {
        public async Task<ResponsePostDto> CreateAsync(Guid userId, CreatePostDto dto)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found.");

            var post = new ProviderPost
            {
                ProviderId = provider.Id,
                Caption = dto.Caption,
                ImageUrl = dto.ImageUrl
            };

            db.ProviderPosts.Add(post);
            await db.SaveChangesAsync();

            return MapToDto(post, provider.BusinessName, currentUserId: userId, likesCount: 0, commentsCount: 0, isLiked: false);
        }

        public async Task DeleteAsync(Guid userId, Guid postId)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found.");

            var post = await db.ProviderPosts
                .FirstOrDefaultAsync(p => p.Id == postId && p.ProviderId == provider.Id)
                ?? throw new NotFoundException("Post not found.");

            db.ProviderPosts.Remove(post);
            await db.SaveChangesAsync();
        }

        public async Task<List<ResponsePostDto>> GetByProviderAsync(Guid providerId, Guid? currentUserId)
        {
            var provider = await db.Providers.FindAsync(providerId)
                ?? throw new NotFoundException("Provider not found.");

            var posts = await db.ProviderPosts
                .Where(p => p.ProviderId == providerId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    Post = p,
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count,
                    IsLiked = currentUserId.HasValue && p.Likes.Any(l => l.UserId == currentUserId.Value)
                })
                .ToListAsync();

            return posts.Select(x => MapToDto(x.Post, provider.BusinessName, currentUserId, x.LikesCount, x.CommentsCount, x.IsLiked)).ToList();
        }

        public async Task LikeAsync(Guid userId, Guid postId)
        {
            var postExists = await db.ProviderPosts.AnyAsync(p => p.Id == postId);
            if (!postExists) throw new NotFoundException("Post not found.");

            var alreadyLiked = await db.PostLikes.AnyAsync(l => l.PostId == postId && l.UserId == userId);
            if (alreadyLiked) throw new BadRequestException("You already liked this post.");

            db.PostLikes.Add(new PostLike { PostId = postId, UserId = userId });
            await db.SaveChangesAsync();
        }

        public async Task UnlikeAsync(Guid userId, Guid postId)
        {
            var like = await db.PostLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId)
                ?? throw new NotFoundException("You haven't liked this post.");

            db.PostLikes.Remove(like);
            await db.SaveChangesAsync();
        }

        public async Task<ResponseCommentDto> AddCommentAsync(Guid userId, Guid postId, CreateCommentDto dto)
        {
            var postExists = await db.ProviderPosts.AnyAsync(p => p.Id == postId);
            if (!postExists) throw new NotFoundException("Post not found.");

            var comment = new PostComment
            {
                PostId = postId,
                UserId = userId,
                Content = dto.Content
            };

            db.PostComments.Add(comment);
            await db.SaveChangesAsync();

            var user = await db.Users.FirstAsync(u => u.Id == userId);

            return new ResponseCommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                UserId = userId,
                UserFullName = user.FullName,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<List<ResponseCommentDto>> GetCommentsAsync(Guid postId)
        {
            var comments = await db.PostComments
                .Include(c => c.User)
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(c => new ResponseCommentDto
            {
                Id = c.Id,
                Content = c.Content,
                UserId = c.UserId,
                UserFullName = c.User.FullName,
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task DeleteCommentAsync(Guid userId, Guid commentId)
        {
            var comment = await db.PostComments.FirstOrDefaultAsync(c => c.Id == commentId)
                ?? throw new NotFoundException("Comment not found.");

            // Yalnız öz comment-ini silə bilər
            if (comment.UserId != userId)
                throw new ForbiddenException("You can only delete your own comment.");

            db.PostComments.Remove(comment);
            await db.SaveChangesAsync();
        }

        private static ResponsePostDto MapToDto(ProviderPost p, string providerName, Guid? currentUserId, int likesCount, int commentsCount, bool isLiked) => new()
        {
            Id = p.Id,
            Caption = p.Caption,
            ImageUrl = p.ImageUrl,
            ProviderId = p.ProviderId,
            ProviderBusinessName = providerName,
            LikesCount = likesCount,
            CommentsCount = commentsCount,
            IsLikedByCurrentUser = isLiked,
            CreatedAt = p.CreatedAt
        };
    }
}
