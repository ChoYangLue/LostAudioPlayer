using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public MainWindow()
        {
            InitializeComponent();
        }

        public void play()
        {
            outputDevice.Volume = Volume;
            Console.WriteLine((float)Volume);

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
            // 再生するファイル名
            string fileName = System.Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) + @"\01-ANIMA.flac";

            // ファイル名の拡張子によって、異なるストリームを生成
            audioStream = new AudioFileReader(fileName);

            // コンストラクタを呼んだ際に、Positionが最後尾に移動したため、0に戻す
            audioStream.Position = 0;

            // プレーヤーの生成
            outputDevice = new WaveOutEvent();

            // 音楽ストリームの入力
            outputDevice.Init(audioStream);

            Volume = Properties.Settings.Default.volume_setting;

            VolumeSlider.Value = Volume * 100;
        }

        private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.volume_setting = Volume;
            Properties.Settings.Default.Save();
        }

        /* ボタンとスライダー関連 */
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Volume = (float) (e.NewValue * 0.01);
            outputDevice.Volume = Volume;

            Console.WriteLine("Volume Change: "+Volume.ToString());
        }
    }
}
