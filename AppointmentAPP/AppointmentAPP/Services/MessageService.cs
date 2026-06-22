using AppointmentAPP.Data;
using AppointmentAPP.Dtos.MessageDtos;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class MessageService(AppDbContext db) : IMessageService
    {
        public async Task<ResponseMessageDto> SendAsync(Guid senderId, SendMessageDto dto)
        {
            Conversation conversation;

            if (dto.ConversationId.HasValue)
            {
                conversation = await db.Conversations
                    .Include(c => c.Provider)
                    .FirstOrDefaultAsync(c => c.Id == dto.ConversationId.Value)
                    ?? throw new NotFoundException("Conversation not found.");

                EnsureParticipant(conversation, senderId);
            }
            else if (dto.ProviderId.HasValue)
            {
                var provider = await db.Providers.FirstOrDefaultAsync(p => p.Id == dto.ProviderId.Value)
                    ?? throw new NotFoundException("Provider not found.");

                if (provider.UserId == senderId)
                    throw new BadRequestException("You cannot message yourself.");

                conversation = await db.Conversations .Include(c => c.Provider) 
                .FirstOrDefaultAsync(c => c.ClientId == senderId && c.ProviderId == provider.Id);

                if (conversation is null)
                {
                    conversation = new Conversation { ClientId = senderId, ProviderId = provider.Id };
                    db.Conversations.Add(conversation);
                    await db.SaveChangesAsync();
                    conversation.Provider = provider;
                }
            }
            else
            {
                throw new BadRequestException("Either ConversationId or ProviderId must be provided.");
            }

            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderId = senderId,
                Content = dto.Content
            };

            db.Messages.Add(message);
            conversation.LastMessageAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var sender = await db.Users.FirstAsync(u => u.Id == senderId);
            var receiverId = senderId == conversation.ClientId ? conversation.Provider.UserId : conversation.ClientId;

            return new ResponseMessageDto
            {
                Id = message.Id,
                ConversationId = conversation.Id,
                SenderId = senderId,
                SenderFullName = sender.FullName,
                ReceiverId = receiverId,
                Content = message.Content,
                IsRead = false,
                CreatedAt = message.CreatedAt
            };
        }

        public async Task<List<ResponseConversationDto>> GetMyConversationsAsync(Guid userId)
        {
            var conversations = await db.Conversations
                .Include(c => c.Client)
                .Include(c => c.Provider)
                .Include(c => c.Messages)
                .Where(c => c.ClientId == userId || c.Provider.UserId == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            return conversations.Select(c =>
            {
                var lastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                var unread = c.Messages.Count(m => !m.IsRead && m.SenderId != userId);

                return new ResponseConversationDto
                {
                    Id = c.Id,
                    ClientId = c.ClientId,
                    ClientFullName = c.Client.FullName,
                    ProviderId = c.ProviderId,
                    ProviderBusinessName = c.Provider.BusinessName,
                    LastMessageContent = lastMessage?.Content,
                    LastMessageAt = c.LastMessageAt,
                    UnreadCount = unread
                };
            }).ToList();
        }

        public async Task<List<ResponseMessageDto>> GetConversationMessagesAsync(Guid userId, Guid conversationId)
        {
            var conversation = await db.Conversations
                .Include(c => c.Provider)
                .FirstOrDefaultAsync(c => c.Id == conversationId)
                ?? throw new NotFoundException("Conversation not found.");

            EnsureParticipant(conversation, userId);

            var messages = await db.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return messages.Select(m => new ResponseMessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderFullName = m.Sender.FullName,
                ReceiverId = m.SenderId == conversation.ClientId ? conversation.Provider.UserId : conversation.ClientId,
                Content = m.Content,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            }).ToList();
        }

        public async Task MarkAsReadAsync(Guid userId, Guid conversationId)
        {
            var conversation = await db.Conversations
                .Include(c => c.Provider)
                .FirstOrDefaultAsync(c => c.Id == conversationId)
                ?? throw new NotFoundException("Conversation not found.");

            EnsureParticipant(conversation, userId);

            var unread = await db.Messages
                .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
                .ToListAsync();

            foreach (var m in unread) m.IsRead = true;
            await db.SaveChangesAsync();
        }

        private static void EnsureParticipant(Conversation conversation, Guid userId)
        {
            if (conversation.ClientId != userId && conversation.Provider.UserId != userId)
                throw new ForbiddenException("You are not part of this conversation.");
        }
    }
}