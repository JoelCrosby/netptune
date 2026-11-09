using System.Text;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Execution.Reviews;

// Text the model writes before a tool call is a note to the person waiting; the final reply is JSON, which
// starts at a brace that opens a line or is followed by a quote.
internal sealed class AiReviewProgressFeed
{
    private const int DraftEveryCharacters = 200;
    private const int FlushEveryCharacters = 80;
    private const int MaxTurnNoteCharacters = 600;
    private const int MaxStoredNoteCharacters = 280;

    private static readonly TimeSpan FlushEvery = TimeSpan.FromMilliseconds(100);

    private readonly IReadOnlySet<AiReviewFindingCategory> Categories;
    private readonly TimeProvider Clock;
    private readonly StringBuilder Narration = new();
    private readonly StringBuilder Reply = new();
    private readonly StringBuilder HeldBrace = new();
    private readonly StringBuilder Pending = new();

    private AiReviewDraftReader Drafts;
    private PendingKind PendingType;
    private bool IsPendingNewBlock;
    private long LastFlush;
    private bool IsInReply;
    private bool IsAtLineStart = true;
    private int TurnNoteCharacters;
    private int DraftedCharacters;
    private string Stage = AiReviewStages.Investigating;
    private AiStreamEventType? PreviousType;
    private string? CurrentNote;

    public AiReviewProgressFeed(IReadOnlySet<AiReviewFindingCategory> categories, TimeProvider clock)
    {
        Categories = categories;
        Clock = clock;
        Drafts = new AiReviewDraftReader(categories);
        LastFlush = clock.GetTimestamp();
    }

    public List<string?> CallNotes { get; } = [];

    public List<AiReviewStreamEvent> Observe(AiStreamEvent streamEvent)
    {
        var events = new List<AiReviewStreamEvent>();

        switch (streamEvent.Type)
        {
            case AiStreamEventType.TextDelta:
                TakeText(streamEvent.Text ?? string.Empty, events);
                break;

            case AiStreamEventType.ThinkingDelta:
                TakeThinking(streamEvent.Text ?? string.Empty, events);
                break;

            case AiStreamEventType.ToolStarted:
                StartTool(streamEvent, events);
                break;

            case AiStreamEventType.ToolCompleted:
                FinishTool(streamEvent, events);
                break;

            case AiStreamEventType.TurnCompleted:
                Flush(events);
                events.AddRange(Drafts.Read(Reply.ToString()));
                break;

            case AiStreamEventType.ReplyReset:
                Flush(events);
                StartReplyAgain();
                break;

            default:
                Flush(events);
                break;
        }

        PreviousType = streamEvent.Type;

        return events;
    }

    public List<AiReviewStreamEvent> Complete()
    {
        var events = new List<AiReviewStreamEvent>();

        ReleaseHeldBrace(events);
        Flush(events);

        return events;
    }

    // The draft reader is kept so the findings already shown are not announced a second time.
    private void StartReplyAgain()
    {
        Reply.Clear();
        HeldBrace.Clear();
        IsInReply = false;
        IsAtLineStart = true;
        DraftedCharacters = 0;
    }

    private void TakeText(string text, List<AiReviewStreamEvent> events)
    {
        var narration = new StringBuilder();
        var reply = new StringBuilder();

        foreach (var character in text)
        {
            if (IsInReply)
            {
                reply.Append(character);

                continue;
            }

            if (HeldBrace.Length > 0)
            {
                HoldOrRelease(character, narration, reply);

                continue;
            }

            if (character == '{')
            {
                OpenReplyOrHold(character, reply);

                continue;
            }

            narration.Append(character);
            IsAtLineStart = character == '\n' || (IsAtLineStart && char.IsWhiteSpace(character));
        }

        if (narration.Length > 0)
        {
            AddNote(narration.ToString(), events);
        }

        if (reply.Length > 0)
        {
            AddReply(reply.ToString(), events);
        }
    }

    private void OpenReplyOrHold(char brace, StringBuilder reply)
    {
        if (IsAtLineStart)
        {
            IsInReply = true;
            reply.Append(brace);

            return;
        }

        HeldBrace.Append(brace);
    }

    private void HoldOrRelease(char character, StringBuilder narration, StringBuilder reply)
    {
        if (char.IsWhiteSpace(character))
        {
            HeldBrace.Append(character);

            return;
        }

        if (character == '"')
        {
            IsInReply = true;
            reply.Append(HeldBrace).Append(character);
            HeldBrace.Clear();

            return;
        }

        narration.Append(HeldBrace).Append(character);
        HeldBrace.Clear();
        IsAtLineStart = false;
    }

    private void ReleaseHeldBrace(List<AiReviewStreamEvent> events)
    {
        if (HeldBrace.Length == 0)
        {
            return;
        }

        var held = HeldBrace.ToString();

        HeldBrace.Clear();

        AddNote(held, events);
    }

    private void AddNote(string text, List<AiReviewStreamEvent> events)
    {
        Narration.Append(text);

        var room = MaxTurnNoteCharacters - TurnNoteCharacters;

        if (room <= 0)
        {
            return;
        }

        var shown = text.Length > room ? text[..room] : text;

        TurnNoteCharacters += shown.Length;

        Buffer(PendingKind.Note, shown, isNewBlock: false, events);
    }

    private void AddReply(string text, List<AiReviewStreamEvent> events)
    {
        Flush(events);

        if (Stage != AiReviewStages.Writing)
        {
            Stage = AiReviewStages.Writing;

            events.Add(AiReviewStreamEvent.Staged(Stage));
        }

        Reply.Append(text);

        var isDueDraft = Reply.Length - DraftedCharacters >= DraftEveryCharacters;

        if (!isDueDraft)
        {
            return;
        }

        DraftedCharacters = Reply.Length;

        events.AddRange(Drafts.Read(Reply.ToString()));
    }

    private void TakeThinking(string text, List<AiReviewStreamEvent> events)
    {
        var isNewBlock = PreviousType != AiStreamEventType.ThinkingDelta;

        if (isNewBlock)
        {
            Flush(events);
        }

        Buffer(PendingKind.Thinking, text, isNewBlock, events);
    }

    private void StartTool(AiStreamEvent streamEvent, List<AiReviewStreamEvent> events)
    {
        var label = streamEvent.ToolName ?? string.Empty;
        var isFirstInTurn = streamEvent.IsFirstInTurn ?? true;

        if (isFirstInTurn)
        {
            CurrentNote = CloseTurn(events);
        }

        Flush(events);

        CallNotes.Add(CurrentNote);

        if (Stage == AiReviewStages.Writing)
        {
            Stage = AiReviewStages.Investigating;

            events.Add(AiReviewStreamEvent.Staged(Stage));
        }

        events.Add(AiReviewStreamEvent.ToolStarted(label));
    }

    private void FinishTool(AiStreamEvent streamEvent, List<AiReviewStreamEvent> events)
    {
        var label = streamEvent.ToolName ?? string.Empty;
        var isError = streamEvent.IsError ?? false;

        Flush(events);

        events.Add(AiReviewStreamEvent.ToolFinished(label, isError));
    }

    // A turn that ends in tool calls was never the final reply, so anything taken for the reply was a note.
    private string? CloseTurn(List<AiReviewStreamEvent> events)
    {
        ReleaseHeldBrace(events);

        if (IsInReply)
        {
            AddNote(Reply.ToString(), events);

            Drafts = new AiReviewDraftReader(Categories);
        }

        var note = Narration.ToString().Trim();

        Narration.Clear();
        Reply.Clear();
        IsInReply = false;
        IsAtLineStart = true;
        TurnNoteCharacters = 0;
        DraftedCharacters = 0;

        return note.Length == 0 ? null : AiReviewPrompt.Truncate(note, MaxStoredNoteCharacters);
    }

    private void Buffer(PendingKind kind, string text, bool isNewBlock, List<AiReviewStreamEvent> events)
    {
        var isOtherKind = Pending.Length > 0 && PendingType != kind;

        if (isOtherKind)
        {
            Flush(events);
        }

        if (Pending.Length == 0)
        {
            PendingType = kind;
            IsPendingNewBlock = isNewBlock;
        }

        Pending.Append(text);

        var isFull = Pending.Length >= FlushEveryCharacters;
        var isDue = isFull || Clock.GetElapsedTime(LastFlush) >= FlushEvery;

        if (isDue)
        {
            Flush(events);
        }
    }

    private void Flush(List<AiReviewStreamEvent> events)
    {
        LastFlush = Clock.GetTimestamp();

        if (Pending.Length == 0)
        {
            return;
        }

        var text = Pending.ToString();

        Pending.Clear();

        var flushed = PendingType == PendingKind.Note
            ? AiReviewStreamEvent.Note(text)
            : AiReviewStreamEvent.Thinking(text, IsPendingNewBlock);

        events.Add(flushed);
    }

    private enum PendingKind
    {
        Note,
        Thinking,
    }
}
