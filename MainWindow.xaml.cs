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
 
        public class IconItem
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public BitmapImage ImageSource { get; set; }
        }

        public MainWindow()
        {
            _params = new NotificationParameters(); // Initialize PROMPTLY
            InitializeComponent();
            SetupEnvironment();
            LoadConfig();
            LoadIconDropdown();
            UserIdentityDisplay.Text = $"Logged in as: {Environment.UserName}";
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWin = new HelpWindow();
            helpWin.Owner = this;
            helpWin.ShowDialog();
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
            AppConfig config = null;
            if (File.Exists(_configFilePath))
            {
                var content = File.ReadAllText(_configFilePath);
                config = JsonConvert.DeserializeObject<AppConfig>(content);
                if (config != null)
                {
                    _imageCachePath = config.ImageCachePath ?? _imageCachePath;
                    _scriptPath = config.ScriptPath ?? _scriptPath;
                }
            }
            
            InitializeScenarios(config);
            
            if (config == null)
            {
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            var config = new AppConfig
            {
                ImageCachePath = _imageCachePath,
                ScriptPath = _scriptPath,
                Scenarios = _scenarios // Persistence for scenarios
            };
            File.WriteAllText(_configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private System.Collections.Generic.List<Scenario> _scenarios;

        private void InitializeScenarios(AppConfig config)
        {
            _scenarios = config?.Scenarios ?? new System.Collections.Generic.List<Scenario>();
            
            // Ensure 5 slots
            while (_scenarios.Count < 5)
            {
                _scenarios.Add(new Scenario { Name = $"Scenario {_scenarios.Count + 1}" });
            }
            ScenariosList.ItemsSource = _scenarios;
        }

        private void SaveScenario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is Scenario scenario)
            {
                scenario.Title = TitleInput.Text;
                scenario.Message = MessageInput.Text;
                scenario.Duration = (DurationDropdown.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
                scenario.AppId = AppIdInput.Text;
                scenario.IconPath = _params.IconPath;
                scenario.Name = string.IsNullOrWhiteSpace(scenario.Title) ? "Saved Scenario" : (scenario.Title.Length > 20 ? scenario.Title.Substring(0, 17) + "..." : scenario.Title);

                ScenariosList.Items.Refresh();
                SaveConfig();
                Log($"Scenario '{scenario.Name}' saved.");
            }
        }

        private void RunScenario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is Scenario scenario)
            {
                if (string.IsNullOrWhiteSpace(scenario.Title))
                {
                    Log("Error: Scenario is empty. Save current settings first.");
                    return;
                }

                _params.Title = scenario.Title;
                _params.Body = scenario.Message;
                _params.Duration = scenario.Duration;
                _params.AppId = scenario.AppId;
                _params.IconPath = scenario.IconPath;

                Log($"Running Scenario: {scenario.Name}");
                RunPowerShell();
            }
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
            if (!ValidateForm())
            {
                GenericErrorText.Visibility = Visibility.Visible;
                return;
            }

            GenericErrorText.Visibility = Visibility.Collapsed;
            _params.Title = TitleInput.Text;
            _params.Body = MessageInput.Text;
            _params.Duration = (DurationDropdown.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            _params.AppId = AppIdInput.Text;

            RunPowerShell();
            // MessageInput.Clear(); // Keep the text so the user can run it multiple times without saving
            SaveConfig();
            ValidateForm(); // Refresh validation after clearing message
        }

        private void ValidateInput_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void ValidateInput_Blur(object sender, RoutedEventArgs e)
        {
            ValidateForm();
        }

        private bool ValidateForm()
        {
            // Safety check for early initialization events
            if (TitleInput == null || MessageInput == null || AppIdInput == null || 
                TitleError == null || MessageError == null || AppIdError == null ||
                IconError == null || GenericErrorText == null) 
                return false;

            bool isValid = true;

            // Title Validation
            bool isTitleValid = !string.IsNullOrWhiteSpace(TitleInput.Text);
            TitleError.Visibility = isTitleValid ? Visibility.Collapsed : Visibility.Visible;
            TitleInput.BorderBrush = isTitleValid ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#D2D2D7") : System.Windows.Media.Brushes.Red;
            if (!isTitleValid) isValid = false;

            // Message Validation
            bool isMessageValid = !string.IsNullOrWhiteSpace(MessageInput.Text);
            MessageError.Visibility = isMessageValid ? Visibility.Collapsed : Visibility.Visible;
            MessageInput.BorderBrush = isMessageValid ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#D2D2D7") : System.Windows.Media.Brushes.Red;
            if (!isMessageValid) isValid = false;

            // AppId Validation
            bool isAppIdValid = !string.IsNullOrWhiteSpace(AppIdInput.Text);
            AppIdError.Visibility = isAppIdValid ? Visibility.Collapsed : Visibility.Visible;
            AppIdInput.BorderBrush = isAppIdValid ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#D2D2D7") : System.Windows.Media.Brushes.Red;
            if (!isAppIdValid) isValid = false;

            // Icon Validation
            bool isIconValid = IconDropdown.SelectedItem != null || !string.IsNullOrWhiteSpace(_params?.IconPath);
            IconError.Visibility = isIconValid ? Visibility.Collapsed : Visibility.Visible;
            if (!isIconValid) isValid = false;

            if (isValid) GenericErrorText.Visibility = Visibility.Collapsed;

            return isValid;
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