namespace AiCV.Infrastructure.Services;

public class NoteService(IDbContextFactory<ApplicationDbContext> factory) : INoteService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory = factory;

    public async Task<List<Note>> GetNotesAsync(string userId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context
            .Notes.AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsArchived)
            .OrderByDescending(n => n.IsPinned)
            .ThenBy(n => n.DisplayOrder)
            .ThenByDescending(n => n.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<Note>> GetArchivedNotesAsync(string userId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context
            .Notes.AsNoTracking()
            .Where(n => n.UserId == userId && n.IsArchived)
            .OrderBy(n => n.DisplayOrder)
            .ThenByDescending(n => n.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Note?> GetNoteByIdAsync(int id, string userId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context
            .Notes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
    }

    public async Task<Note> CreateNoteAsync(Note note)
    {
        await using var context = await _factory.CreateDbContextAsync();
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        return note;
    }

    public async Task<Note> UpdateNoteAsync(Note note)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var existingNote = await context.Notes.FirstOrDefaultAsync(n =>
            n.Id == note.Id && n.UserId == note.UserId
        ) ?? throw new InvalidOperationException("Note not found");
        existingNote.Title = note.Title;
        existingNote.Content = note.Content;
        existingNote.Color = note.Color;
        existingNote.IsPinned = note.IsPinned;
        existingNote.IsArchived = note.IsArchived;
        existingNote.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return existingNote;
    }

    public async Task DeleteNoteAsync(int id, string userId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (note is not null)
        {
            context.Notes.Remove(note);
            await context.SaveChangesAsync();
        }
    }

    public async Task<Note> TogglePinAsync(int id, string userId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId) ?? throw new InvalidOperationException("Note not found");
        note.IsPinned = !note.IsPinned;
        note.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return note;
    }

    public async Task<Note> ToggleArchiveAsync(int id, string userId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId) ?? throw new InvalidOperationException("Note not found");
        note.IsArchived = !note.IsArchived;
        if (note.IsArchived)
        {
            note.IsPinned = false; // Unpin when archiving
        }
        note.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return note;
    }

    public async Task UpdateDisplayOrderAsync(string userId, List<int> noteIds)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var notes = await context
            .Notes.Where(n => n.UserId == userId && noteIds.Contains(n.Id))
            .ToListAsync();

        for (var i = 0; i < noteIds.Count; i++)
        {
            var note = notes.FirstOrDefault(n => n.Id == noteIds[i]);
            note?.DisplayOrder = i;
        }

        await context.SaveChangesAsync();
    }
}
