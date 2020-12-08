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
            }
            else
            {
                outputDevice.Pause();
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

            ts = Task.Run(() => {
                SeekMethod();
            });

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
    }
}
