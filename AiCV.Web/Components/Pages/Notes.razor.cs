namespace AiCV.Web.Components.Pages;

public partial class Notes
{
    private List<Note> _notes = [];
    private List<Note> _archivedNotes = [];
    private List<Note> _displayNotes = [];
    private bool _isLoading = true;
    private string _searchText = string.Empty;
    private bool _showArchived;
    private string? _userId;
    private MudDropContainer<Note>? _dropContainer;

   protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(_userId))
        {
            await LoadNotes();
        }

        _isLoading = false;
    }

    private async Task LoadNotes()
    {
        if (string.IsNullOrEmpty(_userId))
            return;

        _isLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var notes = await NoteService.GetNotesAsync(_userId);
            var archived = await NoteService.GetArchivedNotesAsync(_userId);

            _notes = notes;
            _archivedNotes = archived;

            UpdateDisplayNotes();

            _dropContainer?.Refresh();
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error loading notes", Severity.Error);
            Console.WriteLine($"Error loading notes: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void UpdateDisplayNotes()
    {
        var notes = _showArchived ? _archivedNotes : _notes;

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            notes = [.. notes
                .Where(n =>
                    (n.Title?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (
                        n.Content?.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                        ?? false
                    )
                )];
        }

        _displayNotes = [.. notes];
    }

    private string GetNoteZone(Note note)
    {
        if (_showArchived)
            return "archived";
        return note.IsPinned ? "pinned" : "other";
    }

    private void OnSearchChanged(string value)
    {
        _searchText = value;
        UpdateDisplayNotes();
        _dropContainer?.Refresh();
    }

    private void OnShowArchivedChanged(bool value)
    {
        _showArchived = value;
        UpdateDisplayNotes();
        _dropContainer?.Refresh();
    }

    private List<Note> GetFilteredNotes()
    {
        return _displayNotes;
    }

    private async Task OnItemDropped(MudItemDropInfo<Note> dropInfo)
    {
        if (dropInfo.Item is null || string.IsNullOrEmpty(_userId))
            return;

        var note = dropInfo.Item;
        var targetZone = dropInfo.DropzoneIdentifier;

        // Handle zone changes (pin/unpin)
        if (!_showArchived)
        {
            var wasPinned = note.IsPinned;
            var shouldBePinned = targetZone == "pinned";

            if (wasPinned != shouldBePinned)
            {
                await NoteService.TogglePinAsync(note.Id, _userId);
                await LoadNotes();
                return;
            }
        }

        // Update display order based on new position
        var zonedNotes = _displayNotes.Where(n => GetNoteZone(n) == targetZone).ToList();

        // Remove the dragged item from its current position
        zonedNotes.Remove(note);

        // Insert at new position
        var newIndex = Math.Min(dropInfo.IndexInZone, zonedNotes.Count);
        zonedNotes.Insert(newIndex, note);

        // Get all note IDs in order
        var orderedIds = zonedNotes.ConvertAll(n => n.Id);
        await NoteService.UpdateDisplayOrderAsync(_userId, orderedIds);
        await LoadNotes();
    }

    private async Task AddNote()
    {
        var newNote = new Note { UserId = _userId!, Color = "default" };
        var parameters = new DialogParameters<NoteDialog> { { x => x.NoteModel, newNote } };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };

        var dialog = await DialogService.ShowAsync<NoteDialog>(
            Localizer["AddNote"],
            parameters,
            options
        );
        var result = await dialog.Result;

        if (result?.Canceled == false && result.Data is Note createdNote)
        {
            Console.WriteLine($"Dialog closed. Creating note: {createdNote.Title}");
            await NoteService.CreateNoteAsync(createdNote);
            await LoadNotes();
            Snackbar.Add(Localizer["NoteCreated"], Severity.Success);
        }
        else
        {
            Console.WriteLine("Dialog canceled or result data is not a Note");
        }
    }

    private async Task EditNote(Note note)
    {
        var editNote = new Note
        {
            Id = note.Id,
            UserId = note.UserId,
            Title = note.Title,
            Content = note.Content,
            Color = note.Color,
            IsPinned = note.IsPinned,
            IsArchived = note.IsArchived,
        };

        var parameters = new DialogParameters<NoteDialog> { { x => x.NoteModel, editNote } };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };

        var dialog = await DialogService.ShowAsync<NoteDialog>(
            Localizer["EditNote"],
            parameters,
            options
        );
        var result = await dialog.Result;

        if (result?.Canceled == false && result.Data is Note updatedNote)
        {
            await NoteService.UpdateNoteAsync(updatedNote);
            await LoadNotes();
            Snackbar.Add(Localizer["NoteUpdated"], Severity.Success);
        }
    }

    private async Task TogglePin(Note note)
    {
        var result = await NoteService.TogglePinAsync(note.Id, _userId!);
        await LoadNotes();
        Snackbar.Add(
            result.IsPinned ? Localizer["NotePinned"] : Localizer["NoteUnpinned"],
            Severity.Info
        );
    }

    private async Task ToggleArchive(Note note)
    {
        var result = await NoteService.ToggleArchiveAsync(note.Id, _userId!);
        await LoadNotes();
        Snackbar.Add(
            result.IsArchived ? Localizer["NoteArchived"] : Localizer["NoteUnarchived"],
            Severity.Info
        );
    }

    private async Task DeleteNote(Note note)
    {
        var confirm = await DialogService.ShowMessageBox(
            Localizer["DeleteNote"],
            Localizer["DeleteNoteConfirmation"],
            yesText: Localizer["Delete"],
            cancelText: Localizer["Cancel"]
        );

        if (confirm == true)
        {
            await NoteService.DeleteNoteAsync(note.Id, _userId!);
            await LoadNotes();
            Snackbar.Add(Localizer["NoteDeleted"], Severity.Success);
        }
    }
}
