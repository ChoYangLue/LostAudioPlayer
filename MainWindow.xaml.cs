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
        private DispatcherTimer _timer = new DispatcherTimer();
        private const int _timerInterval = 1;
        private int _elapsedSec = 0;
        private bool _sliderValueChangedByProgram = false;

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
                Console.WriteLine(i.ToString() + "\t" + f.GetDetailsOf(null, i) + "\t" + f.GetDetailsOf(item, i));
                if (f.GetDetailsOf(null, i) == tag_name) return f.GetDetailsOf(item, i);
            }

            return "";
        }

        public void play()
        {
            outputDevice.Volume = Volume;

            if (IsPlaying)
            {
                // 音楽の再生 (おそらく非同期処理)
                outputDevice.Play();

                _timer.Start();
            }
            else
            {
                outputDevice.Pause();

                _timer.Stop();
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
            _elapsedSec += _timerInterval;
            double totalSec = audioStream.TotalTime.TotalSeconds;
            SeekSlider.Value = _elapsedSec / totalSec * SeekSlider.Maximum;

            //double currentSec1 = audioStream.CurrentTime.TotalSeconds;
            //SeekSlider.Value = (currentSec1 / audioStream.TotalTime.TotalSeconds) * SeekSlider.Maximum;
            _sliderValueChangedByProgram = true;
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

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            BoolSwitcher(ref IsPlaying);
            play();
        }

        /* 設定のロードとセーブ関連 */
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string root_folder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            // 再生するファイル名
            string fileName = root_folder + @"\ソードアート・オンライン_アリシゼーション_OP2.wav";

            // ファイル名の拡張子によって、異なるストリームを生成
            audioStream = new AudioFileReader(fileName);

            // コンストラクタを呼んだ際に、Positionが最後尾に移動したため、0に戻す
            audioStream.Position = 0;

            // プレーヤーの生成
            outputDevice = new WaveOutEvent();

            // 音楽ストリームの入力
            outputDevice.Init(audioStream);

            var audio_files = SearchAudioFile(root_folder);

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

            /*
            ts = Task.Run(() => {
                SeekMethod();
            });
            */

            _timer.Interval = new TimeSpan(0, 0, _timerInterval);
            _timer.Tick += dispatcherTimer_Tick;

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
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Volume = (float) (e.NewValue /100 );
            outputDevice.Volume = Volume;

            Console.WriteLine("Volume Change: "+Volume.ToString());
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_sliderValueChangedByProgram && audioStream != null)
            {
                // スライダーを動かした位置に合わせて動画の再生箇所を更新する
                double totalSec = audioStream.TotalTime.TotalSeconds;
                double sliderValue = SeekSlider.Value;
                double targetSec = (sliderValue * totalSec) / SeekSlider.Maximum;
                _elapsedSec = (int) targetSec;
                audioStream.CurrentTime = TimeSpan.FromSeconds(targetSec);
                Console.WriteLine(targetSec);
            }
            _sliderValueChangedByProgram = false;
        }
    }
}
