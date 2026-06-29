using Avalonia.Controls;
using Avalonia.Input;

namespace AA.Annotate.App.Views;

public partial class CommentEditor : UserControl
{
    public event EventHandler? DeleteRequested;

    public event EventHandler<string>? SaveRequested;

    public CommentEditor()
    {
        InitializeComponent();
        DeleteButton.Click += (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty);
        OkButton.Click += (_, _) => SaveRequested?.Invoke(this, CommentTextBox.Text ?? string.Empty);
        CommentTextBox.KeyDown += OnCommentTextBoxKeyDown;
        CommentTextBox.TextChanged += (_, _) => UpdateTextHeight();
        CommentTextBox.GotFocus += (_, _) => TextHostBorder.Classes.Set("focused", true);
        CommentTextBox.LostFocus += (_, _) => TextHostBorder.Classes.Set("focused", false);
    }

    public void Open(string text)
    {
        CommentTextBox.Text = text;
        UpdateTextHeight();
        CommentTextBox.Focus();
        CommentTextBox.SelectAll();
    }

    private void OnCommentTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            InsertLineBreak();
            return;
        }

        SaveRequested?.Invoke(this, CommentTextBox.Text ?? string.Empty);
    }

    private void InsertLineBreak()
    {
        var text = CommentTextBox.Text ?? string.Empty;
        var selectionStart = Math.Clamp(Math.Min(CommentTextBox.SelectionStart, CommentTextBox.SelectionEnd), 0, text.Length);
        var selectionEnd = Math.Clamp(Math.Max(CommentTextBox.SelectionStart, CommentTextBox.SelectionEnd), 0, text.Length);
        CommentTextBox.Text = text[..selectionStart] + Environment.NewLine + text[selectionEnd..];
        CommentTextBox.CaretIndex = selectionStart + Environment.NewLine.Length;
    }

    private void UpdateTextHeight()
    {
        var hostWidth = TextHostBorder.Bounds.Width > 0 ? TextHostBorder.Bounds.Width : 360;
        var charactersPerLine = Math.Max(24, (int)Math.Floor(hostWidth / 7.2));
        var height = CommentEditorLayout.CalculateTextHostHeight(CommentTextBox.Text, charactersPerLine);
        TextHostBorder.Height = height;
        CommentTextBox.Height = Math.Max(34, height - 2);
    }
}
