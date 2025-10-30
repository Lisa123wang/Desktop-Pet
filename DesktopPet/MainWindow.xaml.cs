using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DesktopPet
{
    public partial class MainWindow : Window
    {
        double dx = 2; // 移動速度
        DispatcherTimer timer;
        private bool isDragging = false;

        public MainWindow()
        {
            InitializeComponent();

            // 起始位置 - 在螢幕底部
            Left = 100;
            Top = SystemParameters.PrimaryScreenHeight - Height - 50; // 距離底部50像素

            // 定時器設定
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(30);
            timer.Tick += MovePet;
            timer.Start();
        }

        private void MovePet(object? sender, EventArgs e)
        {
            // 只有在非拖曳狀態下才自動移動
            if (!isDragging)
            {
                Left += dx;

                // 確保保持在底部位置
                Top = SystemParameters.PrimaryScreenHeight - Height - 50;

                // 到邊界反彈
                if (Left + Width >= SystemParameters.PrimaryScreenWidth || Left <= 0)
                {
                    dx = -dx;
                }
            }
        }

        private void Pet_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                isDragging = true;
                this.DragMove(); // 使用內建的拖曳功能
                isDragging = false;
                // 拖曳完成後自動重新開始移動
                if (!timer.IsEnabled)
                {
                    timer.Start();
                }
            }
        }



        private void StartMovement_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            Application.Current.Shutdown();
        }
    }
}
