namespace AiCV.Application.Interfaces;

public interface INoteService
{
    Task<List<Note>> GetNotesAsync(string userId);
    Task<List<Note>> GetArchivedNotesAsync(string userId);
    Task<Note?> GetNoteByIdAsync(int id, string userId);
    Task<Note> CreateNoteAsync(Note note);
    Task<Note> UpdateNoteAsync(Note note);
    Task DeleteNoteAsync(int id, string userId);
    Task<Note> TogglePinAsync(int id, string userId);
    Task<Note> ToggleArchiveAsync(int id, string userId);
    Task UpdateDisplayOrderAsync(string userId, List<int> noteIds);
}
