using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VarispeedDemo.SoundTouch;
using System.Linq;
using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace AudioPlayer
{
    public class MicInfo
    {
        public int DeviceNumber { get; }
        public string ProductName { get; }

        public MicInfo(int deviceNumber, string productName) 
        {
         DeviceNumber = deviceNumber;
         ProductName=productName ;
        }
        public override string ToString()
        {
            return ProductName;
        }
    }

    public partial class MainWindow : Window
    {
        private AudioFileReader audioFileReader;
        private WaveOut player;
        private Timer timer;
        private VarispeedSampleProvider speedControl;
        private WaveIn waveIn;
        private WaveFileWriter writer;
        private ObservableCollection<MicInfo> mics;

        public MainWindow()
        {
            InitializeComponent();

            timer = new Timer(100);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            DataContext = this;

            FillFileComboBox();
            SpeedComboBox.SelectionChanged += SpeedComboBox_SelectionChanged;

        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveIn != null)
            {
                writer.Write(e.Buffer, 0, e.Buffer.Length);   
            }
        }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (audioFileReader != null)
            {
                Dispatcher.Invoke(() =>
                {
                    if (player != null && player.PlaybackState == PlaybackState.Playing)
                    {
                        PositionSlider.ValueChanged -= PositionSlider_ValueChanged;
                        PositionSlider.Value = audioFileReader.Position;
                        UpdateTimeLabel();
                        PositionSlider.ValueChanged += PositionSlider_ValueChanged;
                    }
                });
            }
        }

        private void FileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = (ComboBoxItem)FileComboBox.SelectedItem;
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media", selectedItem.Content.ToString());

            if (System.IO.File.Exists(filePath))
            {
                if (player != null)
                {
                    player.Stop();
                    player.Dispose();
                    speedControl.Dispose();
                }
                timer?.Stop();
                PositionSlider.Value = 0;

                audioFileReader = new AudioFileReader(filePath);
                speedControl = new VarispeedSampleProvider(audioFileReader, 100, new SoundTouchProfile(false, false));

                SpeedComboBox.SelectedIndex = 2;
                player = new WaveOut();
                player.Init(speedControl);

                PositionSlider.Maximum = audioFileReader.Length;
                VolumeSlider.Value = 0.5;
                VolumeLabel.Text = $"{VolumeSlider.Value * 100:0}%";

                UpdateTimeLabel();

                timer.Start();
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            player?.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            player?.Pause();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player != null)
            {
                player.Volume = (float)VolumeSlider.Value;
                VolumeLabel.Text = $"{VolumeSlider.Value * 100:0}%";
            }
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (audioFileReader != null)
            {
                long position = (long)PositionSlider.Value;
                audioFileReader.Position = position;
                UpdateTimeLabel();
            }
        }

        private void UpdateTimeLabel()
        {
            if (audioFileReader != null)
            {
                TimeLabel.Text = audioFileReader.CurrentTime.ToString(@"mm\:ss") + " / " + audioFileReader.TotalTime.ToString(@"mm\:ss");
            }
        }

        private void FillFileComboBox()
        {
            string mediaFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media");

            if (System.IO.Directory.Exists(mediaFolder))
            {
                var audioFiles = System.IO.Directory.GetFiles(mediaFolder)
                                    .Where(file => file.EndsWith(".wav") || file.EndsWith(".mp3") || file.EndsWith(".avi") || file.EndsWith(".m4a"));

                foreach (string file in audioFiles)
                {
                    string fileName = System.IO.Path.GetFileName(file);
                    FileComboBox.Items.Add(new ComboBoxItem() { Content = fileName });
                }
            }
        }

        private void SpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (player != null && audioFileReader != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)SpeedComboBox.SelectedItem;
                switch (selectedItem.Content.ToString())
                {
                    case "x0.5":
                        speedControl.PlaybackRate = 0.5f;
                        break;
                    case "x0.75":
                        speedControl.PlaybackRate = 0.75f;
                        break;
                    case "x1":
                        speedControl.PlaybackRate = 1f;
                        break;
                    case "x1.25":
                        speedControl.PlaybackRate = 1.25f;
                        break;
                    case "x1.5":
                        speedControl.PlaybackRate = 1.5f;
                        break;
                    default:
                        speedControl.PlaybackRate = 1f;
                        break;
                }
            }
        }


        private void ResetPlayer()
        {
            if (player != null)
            {
                player.Stop();
                player.Dispose();
                player = null;
                speedControl.Dispose();

                audioFileReader.Dispose();
                audioFileReader = null;

                timer?.Stop();
                PositionSlider.Value = 0;
                TimeLabel.Text = "";
                VolumeSlider.Value = 0.5;
                VolumeLabel.Text = "50%";

                FileComboBox.SelectionChanged -= FileComboBox_SelectionChanged;
                FileComboBox.SelectedIndex = -1;
                FileComboBox.SelectionChanged += FileComboBox_SelectionChanged;

            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPlayer();
        }


        private void FillMicroComboBox()
        {
            MicroComboBox.Items.Clear();
            mics = new ObservableCollection<MicInfo>();

            for (int waveInDevice = 0; waveInDevice < WaveIn.DeviceCount; waveInDevice++)
            {
                var sourceInfo = new MicInfo(waveInDevice, WaveIn.GetCapabilities(waveInDevice).ProductName);

                mics.Add(sourceInfo);
                MicroComboBox.Items.Add(sourceInfo);
            }
        }

        private void ModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            ModeLabel.Text = "Запись";
            ResetPlayer();
            RecordControls.Visibility = Visibility.Visible;
            FileComboBox.Visibility = Visibility.Collapsed;
            MicroTools.Visibility = Visibility.Visible;

            FillMicroComboBox();

        }


        private void ModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ModeLabel.Text = "Воспроизведение";
            FileComboBox.Visibility = Visibility.Visible;
            RecordControls.Visibility = Visibility.Collapsed;
            MicroTools.Visibility = Visibility.Collapsed;



            ResetPlayer();
        }

        private void StartRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (MicroComboBox.Items.Count > 0)
            {
                MicInfo selectedMic = (MicInfo)MicroComboBox.SelectedItem;

                waveIn = new WaveIn();
                waveIn.WaveFormat = new WaveFormat(44100, 1);
                waveIn.DeviceNumber = selectedMic.DeviceNumber;

                waveIn.DataAvailable += WaveIn_DataAvailable;

                writer = new WaveFileWriter(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.wav"), waveIn.WaveFormat);

                waveIn.StartRecording();
            }

        }

        private void StopRecordButton_Click(object sender, RoutedEventArgs e)
        {
            waveIn.StopRecording();
            waveIn.Dispose();

            writer.Close();
            writer.Dispose();

            if (player != null)
            {
                player.Stop();
                player.Dispose();
                speedControl.Dispose();
            }
            timer?.Stop();
            PositionSlider.Value = 0;

            audioFileReader = new AudioFileReader(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.wav"));
            speedControl = new VarispeedSampleProvider(audioFileReader, 100, new SoundTouchProfile(false, false));

            SpeedComboBox.SelectedIndex = 2;
            player = new WaveOut();
            player.Init(speedControl);

            PositionSlider.Maximum = audioFileReader.Length;
            VolumeSlider.Value = 0.5;
            VolumeLabel.Text = $"{VolumeSlider.Value * 100:0}%";

            UpdateTimeLabel();

            timer.Start();
        }

        private void SaveRecordButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}