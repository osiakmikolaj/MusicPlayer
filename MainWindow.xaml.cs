using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MusicPlayer
{
    public partial class MainWindow : Window
    {
        MediaPlayer mediaPlayer = new MediaPlayer();
        string fileName;
        DispatcherTimer timer; // Timer for updating the progress bar

        public MainWindow()
        {
            InitializeComponent();
            EnsureMusicDirectoryExists();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            LoadPlaylists();
        }

        private void EnsureMusicDirectoryExists()
        {
            string musicFolder = @"C:\music\";
            if (!Directory.Exists(musicFolder))
            {
                Directory.CreateDirectory(musicFolder);
            }
        }
        
        private void LoadPlaylists()
        {
            string musicFolder = @"C:\music\";
            string[] directories = Directory.GetDirectories(musicFolder);
            playlistBox.Items.Clear(); // Clear existing items

            foreach (var directory in directories)
            {
                var dirName = System.IO.Path.GetFileName(directory);
                ListBoxItem item = new ListBoxItem
                {
                    Content = dirName,
                    Tag = directory, // Store the full path in the Tag property
                    FontWeight = FontWeights.Bold // Optional: Make folder names bold
                };
                playlistBox.Items.Add(item); // Add the ListBoxItem to the ListBox
            }
        }

        private void playlistBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (playlistBox.SelectedItem != null)
            {
                var selectedItem = playlistBox.SelectedItem as ListBoxItem;
                if (selectedItem != null)
                {
                    string path = selectedItem.Tag.ToString();
                    if (Directory.Exists(path)) // It's a folder
                    {
                        // Load the contents of the folder into playlistBox
                        string[] filePaths = Directory.GetFiles(path, "*.mp3");
                        playlistBox.Items.Clear(); // Clear existing items

                        // Optional: Add an item to go back to the playlist overview
                        ListBoxItem backItem = new ListBoxItem
                        {
                            Content = "..", // Or any other indicator you prefer
                            Tag = "back",
                            FontWeight = FontWeights.Bold
                        };
                        playlistBox.Items.Add(backItem);

                        foreach (var filePath in filePaths)
                        {
                            var fileName = System.IO.Path.GetFileName(filePath);
                            ListBoxItem item = new ListBoxItem
                            {
                                Content = fileName,
                                Tag = filePath // Store the full path in the Tag property
                            };
                            playlistBox.Items.Add(item); // Add the ListBoxItem to the ListBox
                        }
                    }
                    else if (File.Exists(path)) // It's a file
                    {
                        // Play the file
                        fileName = path;
                        txtTitle.Text = System.IO.Path.GetFileName(fileName);
                        mediaPlayer.Open(new Uri(fileName));
                        mediaPlayer.MediaOpened += (s, e) =>
                        {
                            progressBar.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                            txtDuration.Text = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
                        };
                        mediaPlayer.Play(); // Play the selected song
                        timer.Start(); // Start timer when a song is selected from the playlist
                    }
                    else if (path == "back")
                    {
                        // Go back to the playlist overview
                        LoadPlaylists();
                    }
                }
            }
        }

        private void btnAddPlaylist_Click(object sender, RoutedEventArgs e)
        {
            // Prompt the user for a playlist name
            string playlistName = Microsoft.VisualBasic.Interaction.InputBox("Enter Playlist Name:", "New Playlist", "DefaultName");
            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                string musicFolder = @"C:\music\";
                string newPlaylistPath = System.IO.Path.Combine(musicFolder, playlistName);

                // Check if the folder already exists
                if (!Directory.Exists(newPlaylistPath))
                {
                    // Create the new folder
                    Directory.CreateDirectory(newPlaylistPath);

                    // Optionally, refresh the playlist display to include the new folder
                    LoadPlaylists();
                }
                else
                {
                    MessageBox.Show("A playlist with this name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (playlistBox.SelectedItem != null)
            {
                var selectedItem = playlistBox.SelectedItem as ListBoxItem;
                if (selectedItem != null)
                {
                    string path = selectedItem.Tag.ToString();
                    var isDirectory = Directory.Exists(path);
                    var isFile = File.Exists(path);
                    string itemType = isDirectory ? "playlist" : "file";
                    var result = MessageBox.Show($"Are you sure you want to delete this {itemType}: '{selectedItem.Content}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            if (isDirectory)
                            {
                                Directory.Delete(path, true); // true for recursive delete
                            }
                            else if (isFile)
                            {
                                File.Delete(path);
                            }

                            LoadPlaylists(); // Refresh the playlist display or file list
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting {itemType}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select an item to delete.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btnAddFileToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (playlistBox.SelectedItem != null && playlistBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag.ToString() != "back")
            {
                string playlistPath = selectedItem.Tag.ToString();
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    DefaultExt = ".mp3",
                    Filter = "MPE files (*.mp3)|*.mp3"
                };
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    string selectedFilePath = openFileDialog.FileName;
                    string destinationPath = System.IO.Path.Combine(playlistPath, System.IO.Path.GetFileName(selectedFilePath));
                    try
                    {
                        File.Copy(selectedFilePath, destinationPath, true); // true to overwrite if file already exists
                        MessageBox.Show("File added to playlist successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding file to playlist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a playlist first.", "No Playlist Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                DefaultExt = ".mp3",
            };
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                fileName = openFileDialog.FileName;
                txtTitle.Text = System.IO.Path.GetFileName(fileName);
                mediaPlayer.Open(new Uri(fileName));
                mediaPlayer.MediaOpened += (s, e) =>
                {
                    progressBar.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    txtDuration.Text = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
                };
            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Play();
            timer.Start();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
            timer.Stop();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            timer.Stop();
            progressBar.Value = 0; // Reset progress bar when stopped
            txtCurrentTime.Text = "00:00"; // Reset current time when stopped
            txtDuration.Text = "00:00"; // Reset duration when stopped
        }
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = volumeSlider.Value;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressBar.Value = mediaPlayer.Position.TotalSeconds;
                txtCurrentTime.Text = mediaPlayer.Position.ToString(@"mm\:ss");
            }
        }
    }
}