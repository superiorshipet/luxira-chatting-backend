using InternalChat.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace InternalChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly Infrastructure.Persistence.AppDbContext _db;

    public MessageController(IMessageService messageService, Infrastructure.Persistence.AppDbContext db)
    {
        _messageService = messageService;
        _db = db;
    }
    
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{messageId}/history")]
    public async Task<IActionResult> GetEditHistory(Guid messageId)
    {
        try
        {
            var userId = GetUserId();
            var history = await _messageService.GetMessageEditHistoryAsync(messageId, userId);
            return Ok(history);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Toggle a message as favorite/unfavorite.</summary>
    [HttpPost("{messageId}/favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid messageId)
    {
        var userId = GetUserId();
        var existing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            _db.UserFavoriteMessages, f => f.UserId == userId && f.MessageId == messageId);

        if (existing != null)
        {
            _db.UserFavoriteMessages.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Message removed from favorites.", isFavorite = false });
        }
        else
        {
            // Verify message exists
            var msgExists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                _db.Messages, m => m.Id == messageId);
            if (!msgExists) return NotFound("Message not found.");

            await _db.UserFavoriteMessages.AddAsync(new Domain.Entities.UserFavoriteMessage
            {
                UserId = userId,
                MessageId = messageId
            });
            await _db.SaveChangesAsync();
            return Ok(new { message = "Message added to favorites.", isFavorite = true });
        }
    }

    /// <summary>Get all favorited messages of the current user.</summary>
    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
    {
        var userId = GetUserId();
        var favorites = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _db.UserFavoriteMessages
                .Where(f => f.UserId == userId)
                .Include(f => f.Message)
                .ThenInclude(m => m!.Sender)
                .Select(f => f.Message)
        );

        var result = favorites.Select(m => new {
            m!.Id,
            m.GroupId,
            m.SenderId,
            SenderName = m.Sender?.FullName,
            m.Content,
            m.MessageType,
            m.SentAt,
            m.IsEdited,
            m.IsPinned
        });

        return Ok(result);
    }
}
