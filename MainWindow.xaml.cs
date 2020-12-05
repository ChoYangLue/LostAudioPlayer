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

namespace AudioCenter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsPlaying = false;
        WaveStream audioStream;
        IWavePlayer outputDevice;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void play()
        {
            outputDevice.Volume = 0.009f;


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
        }
    }
}
