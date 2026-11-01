using System.Text.Json;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

/// Invoice.AI assistant: single-intent and multi-intent questions.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class AiView : UserControl
{
    private readonly ApiClient _api;
    private readonly TextBox _question = new() { Multiline = true, BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, Font = Theme.Body, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _multi = new() { Text = "Multiple intent", AutoSize = true, Margin = new Padding(8, 9, 0, 0), ForeColor = Theme.TextPrimary, BackColor = Color.Transparent };
    private readonly ModernButton _ask;
    private readonly TextBox _output = new() { Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 10), WordWrap = false };

    public AiView(ApiClient api)
    {
        _api = api;
        DoubleBuffered = true;
        BackColor = Theme.AppBg;
        Padding = new Padding(24);
        _ask = Ui.Button("Ask", async (_, _) => await AskAsync());

        // answer card (fills the rest)
        var answerCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14, 14, 14, 20) };
        answerCard.Controls.Add(_output);

        // question card
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(0, 6, 0, 0), BackColor = Color.Transparent };
        bar.Controls.Add(_ask);
        bar.Controls.Add(_multi);
        var questionCard = new CardPanel { Dock = DockStyle.Top, Height = 190, Padding = new Padding(14, 14, 14, 18) };
        questionCard.Controls.Add(_question);
        questionCard.Controls.Add(bar);

        var spacer = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };
        var title = new Label { Text = "AI Assistant", Font = Theme.Heading, Dock = DockStyle.Top, Height = 50, ForeColor = Theme.TextPrimary };

        Controls.Add(answerCard);
        Controls.Add(spacer);
        Controls.Add(questionCard);
        Controls.Add(title);
    }

    async Task AskAsync()
    {
        if (string.IsNullOrWhiteSpace(_question.Text)) { Ui.Warn("Type a question first."); return; }
        var route = _multi.Checked ? "api/invoice-ai/ask-multi" : "api/invoice-ai/ask";   // adjust if yours is versioned
        _ask.Enabled = false;
        _output.Text = "Thinking... (a local model can take 10-20 seconds)";
        try
        {
            await Ui.RunAsync(async () =>
            {
                var raw = await _api.PostRawAsync(route, new { question = _question.Text.Trim() });
                _output.Text = Pretty(raw);
            });
        }
        finally { _ask.Enabled = true; }
    }

    static string Pretty(string raw)
    {
        try { return JsonSerializer.Serialize(JsonDocument.Parse(raw).RootElement, new JsonSerializerOptions { WriteIndented = true }); }
        catch { return raw; }
    }
}
