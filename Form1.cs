using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
namespace UxPlay_GUI
{
    public partial class Visualizer : Form
    {
        private Process uxPlayProcess = null!;
        private FileSystemWatcher watcher = null!; // Initialize to null-forgiving operator to suppress CS8618
        private string imageFolder = System.IO.Path.GetTempPath();
        private string uxPlayPath = @"C:\Program Files\UxPlayer AirServer\build";

        public Visualizer()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.FormClosing += Visualizer_FormClosing;
            StartUxPlayProc();
            SetupImageWatch();
        }

        private void StartUxPlayProc()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = $@"{uxPlayPath}\uxplay.exe",
                Arguments = $"-async -ca {imageFolder}uxPlArt.png",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            Process uxPlayProcess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            uxPlayProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ParseUxPlayOutput(e.Data);
                }
            };
            uxPlayProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        Errors.ForeColor = System.Drawing.Color.Red;
                        Errors.Text = e.Data;
                    });
                }
            };
            uxPlayProcess.Start();
            uxPlayProcess.BeginOutputReadLine();
            uxPlayProcess.BeginErrorReadLine();
        }
        private void Visualizer_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = "/IM uxplay.exe /F /T",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var proc = Process.Start(psi);
                proc.WaitForExit(3000); // wait up to 3 seconds for termination
            }
            catch (Exception ex)
            {
                // Optional: log or notify user of failure to kill process
            }
        }

        // Parse UxPlay Output
        private void ParseUxPlayOutput(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            this.Invoke((MethodInvoker)delegate
            {
                switch (data)
                {
                    case var d when d.StartsWith("Title: "):
                        title.Text = d.Substring(7);
                        break;
                    case var d when d.StartsWith("Album: "):
                        Album.Text = d.Substring(7);
                        break;
                    case var d when d.StartsWith("Artist: "):
                        Artist.Text = d.Substring(8);
                        break;
                    case var d when d.StartsWith("Composer: "):
                        composer.Text = d.Substring(10);
                        break;
                    case var d when d.StartsWith("Genre: "):
                        genre.Text = d.Substring(7);
                        break;
                    case var d when d.StartsWith("Local: "):
                        Device.Text = d.Substring(7);
                        break;
                    default:
                        // handle unknown or ignore
                        break;
                }
            });
        }

        // Update Image
        private void SetupImageWatch()
        {
            watcher = new FileSystemWatcher
            {
                Path = imageFolder,
                Filter = "uxPlArt.png",
                NotifyFilter = NotifyFilters.LastWrite
            };
            watcher.Changed += OnImageChanged;
            watcher.EnableRaisingEvents = true;
        }
        private void OnImageChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    using (var stream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBox1.Image = Image.FromStream(stream);
                    }
                });
            }
            catch (Exception ex)
            {
                Errors.ForeColor = System.Drawing.Color.Red;
                Errors.Text = $"Error loading image: {ex.Message}";
            }
        }
    }
}