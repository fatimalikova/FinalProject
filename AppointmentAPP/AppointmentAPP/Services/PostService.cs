using AppointmentAPP.Data;
using AppointmentAPP.Dtos.PostDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class PostService(AppDbContext db, INotificationService notificationService) : IPostService
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
            var post = await db.ProviderPosts
                .Include(p => p.Provider)
                .FirstOrDefaultAsync(p => p.Id == postId)
                ?? throw new NotFoundException("Post not found.");

            var comment = new PostComment { PostId = postId, UserId = userId, Content = dto.Content };
            db.PostComments.Add(comment);
            await db.SaveChangesAsync();

            var user = await db.Users.FirstAsync(u => u.Id == userId);

            // özünə bildiriş getməsin (provider öz postuna comment yazsa)
            if (post.Provider.UserId != userId)
            {
                await notificationService.CreateAsync(
                    post.Provider.UserId, "New Comment",
                    $"{user.FullName} commented on your post.",
                    NotificationType.System);
            }

            // Provider öz postuna şərh yazırsa, biznes şəklini istifadə et
            var imageUrl = userId == post.Provider.UserId
                ? (post.Provider.ImageUrl ?? user.ImageUrl)
                : user.ImageUrl;

            return new ResponseCommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                UserId = userId,
                UserFullName = user.FullName,
                UserImageUrl = imageUrl,
                CreatedAt = comment.CreatedAt
            };
        }


        public async Task<List<ResponseCommentDto>> GetCommentsAsync(Guid postId)
        {
            var post = await db.ProviderPosts
                .Include(p => p.Provider)
                .FirstOrDefaultAsync(p => p.Id == postId)
                ?? throw new NotFoundException("Post not found.");

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
                UserImageUrl = c.UserId == post.Provider.UserId
                    ? (post.Provider.ImageUrl ?? c.User.ImageUrl)
                    : c.User.ImageUrl,
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task DeleteCommentAsync(Guid requestingUserId, Guid commentId, bool isPostOwner = false)
        {
            var comment = await db.PostComments
                .Include(c => c.Post)
                .ThenInclude(p => p.Provider)
                .FirstOrDefaultAsync(c => c.Id == commentId)
                ?? throw new NotFoundException("Comment not found.");

            bool isCommentOwner = comment.UserId == requestingUserId;
            bool isOwnerOfPost = comment.Post.Provider.UserId == requestingUserId;

            if (!isCommentOwner && !isOwnerOfPost)
                throw new ForbiddenException("You can only delete your own comments or comments on your posts.");

            db.PostComments.Remove(comment);
            await db.SaveChangesAsync();
        }

        public async Task<ResponseCommentDto> UpdateCommentAsync(Guid userId, Guid commentId, string content)
        {
            var comment = await db.PostComments
                .Include(c => c.Post)
                .ThenInclude(p => p.Provider)
                .FirstOrDefaultAsync(c => c.Id == commentId)
                ?? throw new NotFoundException("Comment not found.");

            bool isCommentOwner = comment.UserId == userId;
            bool isPostOwner = comment.Post.Provider.UserId == userId;

            if (!isCommentOwner && !isPostOwner)
                throw new ForbiddenException("You cannot edit this comment.");

            comment.Content = content;
            await db.SaveChangesAsync();

            var commentUser = await db.Users.FirstAsync(u => u.Id == comment.UserId);

            var imageUrl = comment.UserId == comment.Post.Provider.UserId
                ? (comment.Post.Provider.ImageUrl ?? commentUser.ImageUrl)
                : commentUser.ImageUrl;

            return new ResponseCommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                UserId = comment.UserId,
                UserFullName = commentUser.FullName,
                UserImageUrl = imageUrl,
                CreatedAt = comment.CreatedAt,
            };
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