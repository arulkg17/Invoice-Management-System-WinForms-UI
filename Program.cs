using Ims.WinForms.Services;
using Ims.WinForms.UI;

namespace Ims.WinForms;

static class Program
{
    // Invoice.CoreAPI base address (trailing slash required)
    const string ApiBaseUrl = "http://localhost:5063/";

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var auth = new AuthState();
        var http = new HttpClient { BaseAddress = new Uri(ApiBaseUrl), Timeout = TimeSpan.FromSeconds(60) };
        var api = new ApiClient(http, auth);

        while (true)
        {
            using (var login = new LoginForm(api))
                if (login.ShowDialog() != DialogResult.OK) return;

            using var main = new MainForm(api, auth);
            Application.Run(main);
            if (!main.LoggedOut) return;      // user closed the window -> exit; logout/expired -> show login again
        }
    }
}
