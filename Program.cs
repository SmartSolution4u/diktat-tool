using System.Windows.Forms;
using DiktatTool;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

var config = Config.Load();

if (string.IsNullOrEmpty(config.GroqApiKey) || !config.GroqApiKey.StartsWith("gsk_"))
{
    using var setup = new SetupDialog();
    if (setup.ShowDialog() != DialogResult.OK)
        return;
    config = Config.Load();
}

Application.Run(new MainApp(config));
