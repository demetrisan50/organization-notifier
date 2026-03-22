using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace organization_notifier
{
    public partial class MainWindow : Window
    {
        private string _appDataPath;
        private string _configFilePath;
        private string _imageCachePath;
        private string _scriptPath;
        private NotificationParameters _params;

        public MainWindow()
        {
            InitializeComponent();
            _params = new NotificationParameters(); // Initialize before loading UI components
            SetupEnvironment();
            LoadConfig();
            LoadIconDropdown();
            UserIdentityDisplay.Text = $"Logged in as: {Environment.UserName}";
        }

        private void SetupEnvironment()
        {
            _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Roaming", "NotificationApp");
            if (!Directory.Exists(_appDataPath)) Directory.CreateDirectory(_appDataPath);

            _configFilePath = Path.Combine(_appDataPath, "notification_config.json");
            _imageCachePath = Path.Combine(_appDataPath, "Cache");
            if (!Directory.Exists(_imageCachePath)) Directory.CreateDirectory(_imageCachePath);

            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Alert.ps1");
            
            // For testing, let's copy Alert.ps1 if it doesn't exist in base dir
            string sourceScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Alert.ps1");
            if (!File.Exists(_scriptPath) && File.Exists(sourceScript))
            {
                File.Copy(sourceScript, _scriptPath);
            }
        }

        private void LoadConfig()
        {
            if (File.Exists(_configFilePath))
            {
                var content = File.ReadAllText(_configFilePath);
                var config = JsonConvert.DeserializeObject<AppConfig>(content);
                if (config != null)
                {
                    _imageCachePath = config.ImageCachePath ?? _imageCachePath;
                    _scriptPath = config.ScriptPath ?? _scriptPath;
                }
            }
            else
            {
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            var config = new AppConfig
            {
                ImageCachePath = _imageCachePath,
                ScriptPath = _scriptPath
            };
            File.WriteAllText(_configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                IconPathDisplay.Text = openFileDialog.FileName;
                ProcessIcon(openFileDialog.FileName);
            }
        }

        public class IconItem
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public BitmapImage ImageSource { get; set; }
        }

        private void LoadIconDropdown()
        {
            var icons = new System.Collections.Generic.List<IconItem>();
            string iconsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
            
            // For development, also check project root
            if (!Directory.Exists(iconsFolder))
            {
                iconsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "icons");
            }

            if (Directory.Exists(iconsFolder))
            {
                foreach (var file in Directory.GetFiles(iconsFolder, "*.png"))
                {
                    var item = new IconItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file,
                        ImageSource = new BitmapImage(new Uri(file))
                    };
                    icons.Add(item);
                }
            }
            IconDropdown.ItemsSource = icons;

            // Select "info" by default if it exists
            foreach (var item in icons)
            {
                if (item.Name.Equals("info", StringComparison.OrdinalIgnoreCase))
                {
                    IconDropdown.SelectedItem = item;
                    break;
                }
            }
        }

        private void IconDropdown_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (IconDropdown.SelectedItem is IconItem selectedIcon)
            {
                IconPathDisplay.Text = selectedIcon.Name;
                ProcessIcon(selectedIcon.Path);
            }
        }

        private void ProcessIcon(string originalPath)
        {
            try
            {
                // Use Cache for icons as requested in early steps
                if (!Directory.Exists(_imageCachePath)) Directory.CreateDirectory(_imageCachePath);
                
                string fileName = Path.GetFileName(originalPath);
                string destinationPath = Path.Combine(_imageCachePath, fileName);
                
                // If it's already in cache or we don't need resizing, we can skip complex processing
                // But the requirement was to resize to 66x66
                BitmapImage bitmap = new BitmapImage(new Uri(originalPath));
                int width = 66;
                int height = 66;

                var resizedImage = new TransformedBitmap(bitmap, new System.Windows.Media.ScaleTransform(
                    (double)width / bitmap.PixelWidth,
                    (double)height / bitmap.PixelHeight));

                using (var fileStream = new FileStream(destinationPath, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(resizedImage));
                    encoder.Save(fileStream);
                }

                _params.IconPath = destinationPath;
                Log($"Icon processed and cached: {destinationPath}");
            }
            catch (Exception ex)
            {
                Log($"Error processing icon: {ex.Message}");
            }
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleInput.Text) || 
                string.IsNullOrWhiteSpace(MessageInput.Text) || 
                string.IsNullOrWhiteSpace(AppIdInput.Text) || 
                IconDropdown.SelectedItem == null)
            {
                MessageBox.Show("All fields (Title, Message, App ID, and Icon) are mandatory.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _params.Title = TitleInput.Text;
            _params.Body = MessageInput.Text;
            _params.Duration = (DurationDropdown.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            _params.AppId = AppIdInput.Text;

            RunPowerShell();
            MessageInput.Clear();
            SaveConfig();
        }

        private void RunPowerShell()
        {
            try
            {
                if (!File.Exists(_scriptPath))
                {
                    Log($"Error: PowerShell script not found at {_scriptPath}");
                    return;
                }

                string executionPolicy = "-ExecutionPolicy Bypass";
                string arguments = $"{executionPolicy} -File \"{_scriptPath}\" -Title \"{_params.Title}\" -Body \"{_params.Body}\" -Duration \"{_params.Duration}\" -AppId \"{_params.AppId}\" -IconPath \"{_params.IconPath}\"";

                // Load config to check for DebugMode
                bool isDebug = false;
                if (File.Exists(_configFilePath))
                {
                    var config = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(_configFilePath));
                    if (config != null) isDebug = config.IsDebugMode;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = isDebug ? $"-NoExit {arguments}" : arguments,
                    UseShellExecute = isDebug, // UseShellExecute true allows window to show up normally
                    RedirectStandardOutput = !isDebug,
                    CreateNoWindow = !isDebug
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (!isDebug)
                    {
                        using (StreamReader reader = process.StandardOutput)
                        {
                            string result = reader.ReadToEnd();
                            Log(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error executing PowerShell: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            LogOutput.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }
    }
}