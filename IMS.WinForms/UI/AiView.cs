using System.Text.Json;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

/// Invoice.AI assistant: single-intent and multi-intent questions.
public sealed class AiView : UserControl
{
    private readonly ApiClient _api;
    private readonly TextBox _question = new() { Multiline = true, Dock = DockStyle.Top, Height = 90, ScrollBars = ScrollBars.Vertical };
    private readonly RadioButton _single = new() { Text = "Single intent", Checked = true, AutoSize = true, Margin = new Padding(0, 8, 12, 0) };
    private readonly RadioButton _multi = new() { Text = "Multiple intent", AutoSize = true, Margin = new Padding(0, 8, 12, 0) };
    private readonly Button _ask;
    private readonly TextBox _output = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 10), WordWrap = false };

    public AiView(ApiClient api)
    {
        _api = api;
        _ask = Ui.Button("Ask", async (_, _) => await AskAsync());

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(0, 6, 0, 0) };
        bar.Controls.Add(_ask);
        bar.Controls.Add(_single);
        bar.Controls.Add(_multi);

        var top = new Panel { Dock = DockStyle.Top, Height = 190, Padding = new Padding(12) };
        top.Controls.Add(bar);
        top.Controls.Add(_question);
        top.Controls.Add(new Label { Text = "AI Assistant", Font = new Font("Segoe UI", 14, FontStyle.Bold), Dock = DockStyle.Top, Height = 34 });

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 12) };
        body.Controls.Add(_output);
        Controls.Add(body);
        Controls.Add(top);
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
