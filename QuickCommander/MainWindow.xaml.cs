﻿using Kennedy.ManagedHooks;
using QuickCommander.API;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;

using Forms = System.Windows.Forms;

namespace QuickCommander
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Output> Outputs { get; set; }

        private bool isVisible;
        private bool isCtrlDown;
        private bool isShiftDown;
        private bool isAltDown;
        private int screenTop;

        public MainWindow(int screenTop)
        {
            InitializeComponent();
            Deactivated += (sender, e) => CloseCommandLine();

            Outputs = new ObservableCollection<Output>();
            DataContext = this;

            this.screenTop = screenTop;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)NativeMethods.GetWindowLong(
                                   wndHelper.Handle,
                                   (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE
                               );

            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(
                wndHelper.Handle,
                (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE,
                (IntPtr)exStyle
            );
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) CloseCommandLine();
            if (e.Key != Key.Enter) return;

            var line = Command.Text.Split(new char[]{' '}, 2);
            var cmd = line[0];
            var args = (line.Length < 2) ? new string[0] : line[1].Split(' ');

            Command.Text = "";

            switch (cmd)
            {
                case "echo":
                    IOManager.Out(this, args[0]);
                    break;

                case "close":
                    CloseCommandLine();
                    break;

                case "quit":
                case "exit":
                    ((App)Application.Current).Shutdown();
                    break;

                default:
                    API.CommandManager.Execute(cmd, args);
                    break;
            }
        }

        public void OnGlobalKeyDown(KeyboardEvents kEvent, Forms.Keys key)
        {
            if (kEvent == KeyboardEvents.KeyDown || kEvent == KeyboardEvents.SystemKeyDown)
            {
                switch (key)
                {
                    case Forms.Keys.Control:
                        isCtrlDown = true;
                        break;

                    case Forms.Keys.Shift:
                        isShiftDown = true;
                        break;
                        
                    case Forms.Keys.Alt:
                        isAltDown = true;
                        break;

                    case Forms.Keys.C:
                        if (isCtrlDown && isShiftDown) ShowCommandLine();
                        break;
                }
            }
            else
            {
                switch (key)
                {
                    case Forms.Keys.Control:
                        isCtrlDown = false;
                        break;

                    case Forms.Keys.Shift:
                        isShiftDown = false;
                        break;

                    case Forms.Keys.Alt:
                        isAltDown = false;
                        break;
                }
            }
        }

        public void OnOutput(object sender, string message)
        {
            var pluginName = (!(sender is MainWindow))
                                ? ((App)Application.Current)
                                      .plugins
                                      .Where(p => sender == p.Instance)
                                      .FirstOrDefault()
                                      .Name
                                : "QuickCommander";

            Outputs.Add(new Output("[" + pluginName + "] " + message));
        }

        public void ShowCommandLine()
        {
            if (isVisible) return;
            isVisible = true;

            Activate();
            ChangeLocation(screenTop - 2, new Duration(TimeSpan.FromMilliseconds(300)));
            Keyboard.Focus(Command);
        }

        public void CloseCommandLine()
        {
            if (!isVisible) return;
            isVisible = false;

            ChangeLocation(screenTop - 49, new Duration(TimeSpan.FromMilliseconds(300)), () => Command.Text = "");
        }

        private void ChangeLocation(double top, Duration duration, Action onCompleted = null)
        {
            var animation = new DoubleAnimation(top, duration);
            if (onCompleted != null) animation.Completed += (sender, e) => onCompleted();

            BeginAnimation(TopProperty, animation);
        }
    }
}