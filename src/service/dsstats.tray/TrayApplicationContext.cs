using System.Reflection;

namespace dsstats.tray
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;
        private static readonly HttpClient client = new();

        public TrayApplicationContext()
        {
            client.BaseAddress = new Uri("http://127.0.0.1:5177/");

            Icon appIcon;
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                var exeDir = Path.GetDirectoryName(exePath);
                if (exeDir != null)
                {
                    var iconPath = Path.Combine(exeDir, "favicon.ico");
                    appIcon = new Icon(iconPath);
                }
                else
                {
                    appIcon = SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                appIcon = SystemIcons.Application;
            }

            trayIcon = new NotifyIcon()
            {
                Icon = appIcon,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = {
                        new ToolStripMenuItem("Trigger Decode", null, TriggerDecode_Click),
                        new ToolStripSeparator(),
                        new ToolStripMenuItem("Stop Service", null, StopService_Click),
                        new ToolStripMenuItem("Exit", null, Exit_Click)
                    }
                },
                Visible = true,
                Text = "DSStats Service"
            };
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        private async void StopService_Click(object? sender, EventArgs e)
        {
            try
            {
                var response = await client.PostAsync("api/service/stop", null);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("DSStats service is stopping.", "Service Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    trayIcon.Visible = false;
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show($"Failed to stop service. Status code: {response.StatusCode}", "Service Control Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping service: {ex.Message}", "Service Control Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void TriggerDecode_Click(object? sender, EventArgs e)
        {
            // TODO: use a fire and forget trigger
            try
            {
                var response = await client.PostAsync("api/service/trigger-import", null);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Decode process triggered.", "Service Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to trigger decode. Status code: {response.StatusCode}", "Service Control Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error triggering decode: {ex.Message}", "Service Control Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}