using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.IO;
using Shell32;

using NAudio.Wave;
using System.Windows.Threading;

namespace LostAudioPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsPlaying = false;
        private float Volume = 0.01f;
        WaveStream audioStream;
        IWavePlayer outputDevice;
        private bool SeekTaskFlag = true;
        Task ts;
        private DispatcherTimer SeekbarTimer = new DispatcherTimer();
        private const int SeekbarTimerIntervalMillisec = 200;
        private bool IsSeekbarChangeByProgram = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        public List<string> SearchAudioFile(string path)
        {
            var audio_files = new List<string>();
            string[] names = Directory.GetFiles(path, "*");
            foreach (string name in names)
            {
                if (System.IO.Path.GetExtension(name) == ".wav" ||
                    System.IO.Path.GetExtension(name) == ".flac" ||
                    System.IO.Path.GetExtension(name) == ".mp3" ||
                    System.IO.Path.GetExtension(name) == ".m4a")
                {
                    Console.WriteLine(name);
                    audio_files.Add(name);
                }
            }
            return audio_files;
        }

        public string GetTagInfoByAudioFile(string file_path, string tag_name)
        {
            Shell shell = new Shell();
            Folder f = shell.NameSpace(System.IO.Path.GetDirectoryName(file_path));
            FolderItem item = f.ParseName(System.IO.Path.GetFileName(file_path));

            for (int i = 0; i < 30; i++)
            {
                //Console.WriteLine(i.ToString() + "\t" + f.GetDetailsOf(null, i) + "\t" + f.GetDetailsOf(item, i));
                if (f.GetDetailsOf(null, i) == tag_name) return f.GetDetailsOf(item, i);
            }

            return "";
        }

        public string GetRootAudioDirectory()
        {
            string root_folder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            return root_folder;
        }

        public void play()
        {
            outputDevice.Volume = Volume;

            if (IsPlaying)
            {
                // 音楽の再生 (おそらく非同期処理)
                outputDevice.Play();
                SeekbarTimer.Start();
            }
            else
            {
                outputDevice.Pause();
                SeekbarTimer.Stop();
            }
        }

        public void SeekMethod()
        {
            while (SeekTaskFlag)
            {
                double currentSec1 = audioStream.CurrentTime.TotalSeconds;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    SeekSlider.Value = (currentSec1/ audioStream.TotalTime.TotalSeconds)*SeekSlider.Maximum;
                }));
                Thread.Sleep(500);
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // 動画経過時間に合わせてスライダーを動かす
            double currentSec1 = audioStream.CurrentTime.TotalSeconds;
            SeekSlider.Value = (currentSec1 / audioStream.TotalTime.TotalSeconds) * SeekSlider.Maximum;
            IsSeekbarChangeByProgram = true;

            if (outputDevice.PlaybackState == PlaybackState.Stopped)
            {
                // Loop
                IsPlaying = false;
                audioStream.Position = 0;
                outputDevice.Play();
                IsPlaying = true;

                Console.WriteLine("おわた");
            }
        }

        private void BoolSwitcher(ref bool switch_button)
        {
            if (switch_button)
            {
                switch_button = false;
                return;
            }
            switch_button = true;
        }

        private void LoadAudioFile(string audio_file_path)
        {
            if (!File.Exists(audio_file_path))
            {
                return;
            }

            // ファイル名の拡張子によって、異なるストリームを生成
            audioStream = new AudioFileReader(audio_file_path);

            // コンストラクタを呼んだ際に、Positionが最後尾に移動したため、0に戻す
            audioStream.Position = 0;

            // プレーヤーの生成
            outputDevice = new WaveOutEvent();

            // 音楽ストリームの入力
            outputDevice.Init(audioStream);
        }

        /* 設定のロードとセーブ関連 */
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var audio_files = SearchAudioFile( GetRootAudioDirectory() );

            ObservableCollection<AudioList> AudioLists = new ObservableCollection<AudioList>();
            foreach (string file in audio_files)
            {
                AudioLists.Add(new AudioList() { 
                    Name = GetTagInfoByAudioFile(file, "名前"), 
                    Length = GetTagInfoByAudioFile(file, "長さ"), 
                    Artist = GetTagInfoByAudioFile(file, "参加アーティスト"), 
                    Album = GetTagInfoByAudioFile(file, "アルバム") 
                });
            }

            AudioListView.ItemsSource = AudioLists;

            // 再生するファイル名
            
            if (audio_files.Count() > 0) LoadAudioFile(audio_files[0]);

            /*
            ts = Task.Run(() => {
                SeekMethod();
            });
            */

            SeekbarTimer.Interval = TimeSpan.FromMilliseconds(SeekbarTimerIntervalMillisec);
            SeekbarTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            Volume = Properties.Settings.Default.volume_setting;

            VolumeSlider.Value = Volume * 100;
        }

        private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SeekTaskFlag = false;

            Properties.Settings.Default.volume_setting = Volume;
            Properties.Settings.Default.Save();
        }

        /* ボタンとスライダー関連 */
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (audioStream == null) return;
            BoolSwitcher(ref IsPlaying);
            play();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Volume = (float) (e.NewValue /100 );
            outputDevice.Volume = Volume;

            Console.WriteLine("Volume Change: "+Volume.ToString());
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsSeekbarChangeByProgram && audioStream != null)
            {
                // スライダーを動かした位置に合わせて動画の再生箇所を更新する
                double totalSec = audioStream.TotalTime.TotalSeconds;
                double sliderValue = SeekSlider.Value;
                double targetSec = (sliderValue * totalSec) / SeekSlider.Maximum;
                audioStream.CurrentTime = TimeSpan.FromSeconds(targetSec);
                Console.WriteLine("skip: "+targetSec.ToString());
            }
            IsSeekbarChangeByProgram = false;
        }

        private void AudioListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AudioListView.SelectedItem == null) return; // ListViewで何も選択されていない場合は何もしない

            AudioList item = (AudioList)AudioListView.SelectedItem; // ListViewで選択されている項目を取り出す
            string Text = GetRootAudioDirectory() + @"\" +item.Name;
            Console.WriteLine(Text);

            outputDevice.Stop();
            SeekbarTimer.Stop();

            LoadAudioFile(Text);
            SeekSlider.Value = 0;
        }
    }
}
