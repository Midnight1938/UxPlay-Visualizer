using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;

namespace UxVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileSystemWatcher watcher = null!;
        private string imageFolder = System.IO.Path.GetTempPath();
        private string uxPlayPath = @"C:\Program Files\UxPlayer AirServer\build";
        private string playName = "Pahadi-Win";
        private string localIp = string.Empty;
        private string remoteIp = string.Empty;
        private string clientInfo = string.Empty;
        private string playbackStatus = string.Empty;

        private WasapiLoopbackCapture? audioCapture;
        // Visualizer bar levels; count can grow with album width
        private double[] barLevels = new double[60];
        private readonly List<Rectangle> visualizerBars = new();

        private DateTime lastVisualizerUpdate = DateTime.MinValue;

        // DWM interop for coloring the title bar on supported Windows versions
        private enum DWMWINDOWATTRIBUTE
        {
            DWMWA_CAPTION_COLOR = 35,
            DWMWA_TEXT_COLOR = 36
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref uint pvAttribute, int cbAttribute);

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;
            MusicVisualizer.Visibility = Visibility.Collapsed;
            VisualizerGrid.Loaded += VisualizerGrid_Loaded;
            SizeChanged += MainWindow_SizeChanged;
            StartUxPlayProc();
            SetupImageWatch();
            StartAudioCapture();
        }

        private void StartUxPlayProc()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = System.IO.Path.Combine(uxPlayPath, "uxplay.exe"),
                Arguments = $"-async -ca {System.IO.Path.Combine(imageFolder, "uxPlArt.png")} -n {playName} -nh",
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
                    Debug.WriteLine(e.Data);
                }
            };

            uxPlayProcess.Start();
            uxPlayProcess.BeginOutputReadLine();
            uxPlayProcess.BeginErrorReadLine();
        }

        private void VisualizerGrid_Loaded(object sender, RoutedEventArgs e)
        {
            BuildVisualizerBars();
        }

        private void StartAudioCapture()
        {
            try
            {
                audioCapture = new WasapiLoopbackCapture();
                audioCapture.DataAvailable += AudioCaptureOnDataAvailable;
                audioCapture.RecordingStopped += (s, e) =>
                {
                    audioCapture?.Dispose();
                    audioCapture = null;
                };
                audioCapture.StartRecording();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio capture start failed: {ex.Message}");
            }
        }

        private void AudioCaptureOnDataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (audioCapture == null || e.BytesRecorded <= 0)
                {
                    return;
                }

                var format = audioCapture.WaveFormat;
                int bytesPerSample = format.BitsPerSample / 8;
                int channels = format.Channels;
                if (bytesPerSample <= 0 || channels <= 0)
                {
                    return;
                }

                int sampleCount = e.BytesRecorded / bytesPerSample / channels;
                if (sampleCount <= 0)
                {
                    return;
                }

                int bars = barLevels.Length;
                int samplesPerBar = sampleCount / bars;
                if (samplesPerBar <= 0)
                {
                    return;
                }

                double[] newLevels = new double[bars];
                int bufferOffset = 0;

                if (format.Encoding == WaveFormatEncoding.IeeeFloat && bytesPerSample == 4)
                {
                    for (int bar = 0; bar < bars; bar++)
                    {
                        double sumSquares = 0;
                        int count = 0;

                        for (int i = 0; i < samplesPerBar; i++)
                        {
                            if (bufferOffset + 4 * channels > e.BytesRecorded)
                            {
                                break;
                            }

                            for (int ch = 0; ch < channels; ch++)
                            {
                                float sample = BitConverter.ToSingle(e.Buffer, bufferOffset + 4 * ch);
                                sumSquares += sample * sample;
                            }

                            count += channels;
                            bufferOffset += 4 * channels;
                        }

                        if (count > 0)
                        {
                            double rms = Math.Sqrt(sumSquares / count);
                            newLevels[bar] = rms;
                        }
                    }
                }
                else if (format.Encoding == WaveFormatEncoding.Pcm && bytesPerSample == 2)
                {
                    for (int bar = 0; bar < bars; bar++)
                    {
                        double sumSquares = 0;
                        int count = 0;

                        for (int i = 0; i < samplesPerBar; i++)
                        {
                            if (bufferOffset + 2 * channels > e.BytesRecorded)
                            {
                                break;
                            }

                            for (int ch = 0; ch < channels; ch++)
                            {
                                short sample = BitConverter.ToInt16(e.Buffer, bufferOffset + 2 * ch);
                                double sample32 = sample / 32768.0;
                                sumSquares += sample32 * sample32;
                            }

                            count += channels;
                            bufferOffset += 2 * channels;
                        }

                        if (count > 0)
                        {
                            double rms = Math.Sqrt(sumSquares / count);
                            newLevels[bar] = rms;
                        }
                    }
                }
                else
                {
                    return;
                }

                double max = 0;
                for (int i = 0; i < bars; i++)
                {
                    if (newLevels[i] > max)
                    {
                        max = newLevels[i];
                    }
                }

                if (max < 1e-9)
                {
                    max = 1e-9;
                }

                for (int i = 0; i < bars; i++)
                {
                    double normalized = Math.Min(1.0, newLevels[i] / max);
                    barLevels[i] = barLevels[i] * 0.7 + normalized * 0.3;
                }

                var now = DateTime.UtcNow;
                if ((now - lastVisualizerUpdate).TotalMilliseconds < 33)
                {
                    return;
                }
                lastVisualizerUpdate = now;

                Dispatcher.BeginInvoke(new Action(UpdateVisualizerBars));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio capture processing failed: {ex.Message}");
            }
        }

        private void UpdateVisualizerBars()
        {
            if (VisualizerGrid == null)
            {
                return;
            }
            if (visualizerBars.Count == 0)
            {
                BuildVisualizerBars();
            }

            // Base vertical range for bar heights
            double minHeight = 2;
            double maxHeight = VisualizerGrid.ActualHeight > 0 ? VisualizerGrid.ActualHeight : 32;
            double range = Math.Max(4, maxHeight - minHeight);

            int count = Math.Min(visualizerBars.Count, barLevels.Length);
            if (count <= 0)
            {
                return;
            }

            // Triangular weight: center bars get full height, edges reduced.
            double center = (count - 1) / 2.0;
            double maxDistance = Math.Max(1.0, center);

            const double amplitudeScale = 1.3;
            const double edgeFloor = 0.25; // edges are at least 25% of center height

            for (int i = 0; i < count; i++)
            {
                var bar = visualizerBars[i];

                // Base level from audio, scaled a bit for more motion
                double level = barLevels[i] * amplitudeScale;
                if (level > 1.0)
                {
                    level = 1.0;
                }

                // Triangular weighting across the bar indices
                double distance = Math.Abs(i - center);
                double tri = 1.0 - (distance / maxDistance); // 1 at center, 0 at edges
                if (tri < 0)
                {
                    tri = 0;
                }

                double weight = edgeFloor + (1.0 - edgeFloor) * tri;
                double shaped = level * weight;

                double h = minHeight + shaped * range;
                bar.Height = h;
            }
        }

        private void BuildVisualizerBars()
        {
            visualizerBars.Clear();
            VisualizerGrid.Children.Clear();
            VisualizerGrid.ColumnDefinitions.Clear();

            int barCount = barLevels.Length;
            for (int i = 0; i < barCount; i++)
            {
                VisualizerGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            Brush brush = (Brush)Resources["VisualizerBarBrush"];

            for (int i = 0; i < barCount; i++)
            {
                var rect = new Rectangle
                {
                    Width = 2,
                    Height = 2,
                    Fill = brush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                Grid.SetColumn(rect, i);
                VisualizerGrid.Children.Add(rect);
                visualizerBars.Add(rect);
            }
        }

        private void EnsureBarCountForWidth(double artWidth)
        {
            if (artWidth <= 0)
            {
                return;
            }

            // Rough rule: about one bar per 8 pixels of album width
            int desired = (int)(artWidth / 8.0);

            // Keep a sensible range so we don't create too many tiny bars
            if (desired < 40)
            {
                desired = 40;
            }
            else if (desired > 140)
            {
                desired = 140;
            }

            if (desired == barLevels.Length)
            {
                return;
            }

            barLevels = new double[desired];
            BuildVisualizerBars();
        }

        private void UpdateBarThicknessForWidth(double artWidth)
        {
            if (artWidth <= 0)
            {
                return;
            }

            if (visualizerBars.Count == 0)
            {
                BuildVisualizerBars();
                if (visualizerBars.Count == 0)
                {
                    return;
                }
            }

            // Use a simple scale relative to a reference width so that
            // bars slowly thicken as the album area gets wider.
            double scale = artWidth / 300.0; // 300px => base scale of 1
            double targetWidth = 2.0 * scale; // base width 2 at ~300px

            if (targetWidth < 2.0)
            {
                targetWidth = 2.0;
            }
            if (targetWidth > 6.0)
            {
                targetWidth = 6.0;
            }

            foreach (var bar in visualizerBars)
            {
                bar.Width = targetWidth;
            }
        }

        private void ParseUxPlayOutput(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var handled = false;
                switch (data)
                {
                    case var d when d.StartsWith("Title: "):
                        Song_Name.Text = d.Substring(7);
                        handled = true;
                        break;
                    case var d when d.StartsWith("Album: "):
                        Album_Name.Text = d.Substring(7);
                        handled = true;
                        break;
                    case var d when d.StartsWith("Artist: "):
                        Artist_Name.Text = d.Substring(8);
                        handled = true;
                        break;
                    default:
                        break;
                }

                if (!handled && IsInterestingStatusLine(data))
                {
                    AppendStatusLine(data);
                }
            });
        }

        private bool IsInterestingStatusLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            // Device/IP-related info
            if (line.StartsWith("Local: ", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Remote: ", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Client identified as", StringComparison.OrdinalIgnoreCase)) return true;

            // Playback status like "Playing" / "Paused" / "Stopped" if UxPlay prints it
            if (line.IndexOf("Playing", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (line.IndexOf("Paused", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (line.IndexOf("Stopped", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        private void AppendStatusLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            const string localPrefix = "Local: ";
            const string remotePrefix = "Remote: ";
            const string clientPrefix = "Client identified as";

            if (line.StartsWith(localPrefix, StringComparison.OrdinalIgnoreCase))
            {
                localIp = line.Substring(localPrefix.Length).Trim();
            }
            else if (line.StartsWith(remotePrefix, StringComparison.OrdinalIgnoreCase))
            {
                remoteIp = line.Substring(remotePrefix.Length).Trim();
            }
            else if (line.StartsWith(clientPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var rest = line.Substring(clientPrefix.Length).Trim();
                clientInfo = string.IsNullOrWhiteSpace(rest) ? line : rest;
            }

            if (line.IndexOf("Playing", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                playbackStatus = "Playing";
            }
            else if (line.IndexOf("Paused", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                playbackStatus = "Paused";
            }
            else if (line.IndexOf("Stopped", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                playbackStatus = "Stopped";
            }

            var ipToShow = !string.IsNullOrWhiteSpace(remoteIp) ? remoteIp : localIp;
            var deviceToShow = !string.IsNullOrWhiteSpace(clientInfo) ? clientInfo : "Device";
            var statusToShow = !string.IsNullOrWhiteSpace(playbackStatus) ? playbackStatus : "Idle";

            if (StatusText != null)
            {
                StatusText.Text = $"{ipToShow} | {deviceToShow} | {statusToShow}";
            }
        }

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
                Dispatcher.Invoke(() =>
                {
                    BitmapImage bitmap = new BitmapImage();
                    using (FileStream stream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;

                        bitmap.EndInit();
                        bitmap.Freeze();
                    }

                    // Foreground album art (keeps full aspect without cropping)
                    Album_Art.Stretch = Stretch.Uniform;
                    Album_Art.Source = bitmap;

                    // Blurred background derived from the same image
                    BackgroundArt.Source = bitmap;

                    // Show simple visualizer when we have album art (music playing)
                    MusicVisualizer.Visibility = Visibility.Visible;

                    // Try to tint the title bar using a color derived from the artwork
                    ApplyCaptionColorFromImage(bitmap);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading image: {ex.Message}");
            }
        }

        private void ApplyCaptionColorFromImage(BitmapSource source)
        {
            try
            {
                // Downsample region for a quick average color
                const int sampleSize = 16;

                var formatted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                int width = Math.Min(sampleSize, formatted.PixelWidth);
                int height = Math.Min(sampleSize, formatted.PixelHeight);
                if (width <= 0 || height <= 0)
                {
                    return;
                }

                int stride = width * 4;
                byte[] pixels = new byte[height * stride];

                int x = (formatted.PixelWidth - width) / 2;
                int y = (formatted.PixelHeight - height) / 2;

                formatted.CopyPixels(new Int32Rect(x, y, width, height), pixels, stride, 0);

                long rTotal = 0, gTotal = 0, bTotal = 0;
                int pixelCount = width * height;

                for (int i = 0; i < pixelCount; i++)
                {
                    int index = i * 4;
                    byte b = pixels[index + 0];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];
                    // byte a = pixels[index + 3]; // alpha not used

                    rTotal += r;
                    gTotal += g;
                    bTotal += b;
                }

                if (pixelCount == 0)
                {
                    return;
                }

                byte rAvg = (byte)(rTotal / pixelCount);
                byte gAvg = (byte)(gTotal / pixelCount);
                byte bAvg = (byte)(bTotal / pixelCount);

                // Also tint the visualizer bars with this average color
                if (Resources["VisualizerBarBrush"] is SolidColorBrush barBrush)
                {
                    barBrush.Color = Color.FromArgb(0xCC, rAvg, gAvg, bAvg);
                }

                // COLORREF is 0x00BBGGRR
                uint captionColor = (uint)(rAvg | (gAvg << 8) | (bAvg << 16));
                uint textColor = 0x00FFFFFF; // white text

                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR, ref captionColor, sizeof(uint));
                DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_TEXT_COLOR, ref textColor, sizeof(uint));
            }
            catch
            {
                // Best-effort: ignore failures (older Windows, missing DWM, etc.)
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                try
                {
                    if (audioCapture != null)
                    {
                        audioCapture.DataAvailable -= AudioCaptureOnDataAvailable;
                        audioCapture.StopRecording();
                        audioCapture.Dispose();
                        audioCapture = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping audio capture: {ex.Message}");
                }

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
                proc?.WaitForExit(3000);
            }
            catch (Exception)
            {
            }
        }

        // Dynamically adjust album art and visualizer sizing on window resize
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AlbumViewbox == null)
            {
                return;
            }

            double totalHeight = ActualHeight;
            double totalWidth = ActualWidth;
            if (totalHeight <= 0 || totalWidth <= 0)
            {
                return;
            }

            // Measure space taken by text and status bar
            double textHeight = Song_Name.ActualHeight + Artist_Name.ActualHeight + Album_Name.ActualHeight;
            if (textHeight <= 0)
            {
                textHeight = 60; // fallback estimate
            }

            double statusHeight = StatusText.ActualHeight;
            if (statusHeight <= 0)
            {
                statusHeight = 24; // typical status bar height
            }

            // Rough vertical margins (top padding, gaps between elements, bottom padding)
            double verticalMargins = 20 + 10 + 20;

            // Always reserve some vertical space specifically for the visualizer row so
            // the album art cannot consume it when the window gets taller.
            // Slightly over-reserve so that horizontal growth starts producing side margins
            // before the art would run into the bottom edge.
            double visualizerReserved = 100; // minimum desired space for bars
            double extraSafety = 20;         // additional cushion for miscellaneous spacing

            double reservedHeight = textHeight + statusHeight + verticalMargins + visualizerReserved + extraSafety;

            double availableForArtByHeight = totalHeight - reservedHeight;
            if (availableForArtByHeight < 150)
            {
                availableForArtByHeight = 150;
            }

            // Allow art to grow with width, but for extremely wide windows we still limit by height
            double minArt = 150;
            double availableForArtByWidth = totalWidth - 200; // leave some side padding
            if (availableForArtByWidth < minArt)
            {
                availableForArtByWidth = minArt;
            }

            // Slightly reduce the effective height budget to leave a bit of extra
            // breathing room at the bottom, avoiding any residual clipping when
            // the window is wide and not very tall.
            double effectiveHeightForArt = availableForArtByHeight * 0.9;

            double cap = Math.Min(effectiveHeightForArt, availableForArtByWidth);
            if (cap < minArt)
            {
                cap = minArt;
            }

            // This keeps the album art mostly driven by height. When the window gets very wide
            // without getting taller, the album stops growing and side margins increase instead.
            // Use the cap as the actual rendered size of the square album area so it visibly scales.
            AlbumViewbox.Width = cap;
            AlbumViewbox.Height = cap;
            AlbumViewbox.MaxWidth = cap;
            AlbumViewbox.MaxHeight = cap;

            // Use the realized album width if we have it; otherwise fall back to the cap.
            double realizedArtWidth = AlbumViewbox.ActualWidth > 0 ? AlbumViewbox.ActualWidth : cap;

            // Adapt visualizer bar count to the current album width
            EnsureBarCountForWidth(realizedArtWidth);

            // And update bar thickness so the bars get a bit thicker as the
            // album art width grows.
            UpdateBarThicknessForWidth(realizedArtWidth);

            // Scale visualizer thickness with the album area size so it looks beefier as the art grows,
            // but never smaller than the reserved space.
            double targetVisualizerMaxHeight = Math.Min(120, cap * 0.35);
            if (targetVisualizerMaxHeight < visualizerReserved)
            {
                targetVisualizerMaxHeight = visualizerReserved;
            }

            MusicVisualizer.MaxHeight = targetVisualizerMaxHeight;
            // Use a larger fraction of the reserved height so bars can grow roughly twice as tall.
            VisualizerGrid.Height = targetVisualizerMaxHeight * 0.70;

            UpdateVisualizerBars();
        }
    }
}