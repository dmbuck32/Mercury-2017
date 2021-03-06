﻿#pragma checksum "..\..\MainWindow.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "8C1CD661F5D527A9F0C966C0F7D33D8A"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using GMap.NET.WindowsForms;
using Mars_Rover_OCU.Properties;
using Mars_Rover_OCU.ValidationRules;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Mars_Rover_OCU {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 15 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid roverGrid;
        
        #line default
        #line hidden
        
        
        #line 64 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox portTxtBox;
        
        #line default
        #line hidden
        
        
        #line 69 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button roverListenBtn;
        
        #line default
        #line hidden
        
        
        #line 81 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button roverControlBtn;
        
        #line default
        #line hidden
        
        
        #line 92 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton manualControlRB;
        
        #line default
        #line hidden
        
        
        #line 93 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton autoControlRB;
        
        #line default
        #line hidden
        
        
        #line 106 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton xboxControlRB;
        
        #line default
        #line hidden
        
        
        #line 107 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton keyControlRB;
        
        #line default
        #line hidden
        
        
        #line 112 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox logConsole;
        
        #line default
        #line hidden
        
        
        #line 127 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label connectionStatusLbl;
        
        #line default
        #line hidden
        
        
        #line 128 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button disconnectRoverBtn;
        
        #line default
        #line hidden
        
        
        #line 129 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid keyboard;
        
        #line default
        #line hidden
        
        
        #line 131 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle forwardBtn;
        
        #line default
        #line hidden
        
        
        #line 132 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle reverseBtn;
        
        #line default
        #line hidden
        
        
        #line 133 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle leftBtn;
        
        #line default
        #line hidden
        
        
        #line 135 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle rightBtn;
        
        #line default
        #line hidden
        
        
        #line 201 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider keyboardSpeedSlider;
        
        #line default
        #line hidden
        
        
        #line 210 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SpeedSensitivity;
        
        #line default
        #line hidden
        
        
        #line 224 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid keyboard_Copy;
        
        #line default
        #line hidden
        
        
        #line 226 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle ArmUpBtn;
        
        #line default
        #line hidden
        
        
        #line 227 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle ArmDownBtn;
        
        #line default
        #line hidden
        
        
        #line 228 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle ScoopInBtn;
        
        #line default
        #line hidden
        
        
        #line 239 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle ScoopOutBtn;
        
        #line default
        #line hidden
        
        
        #line 352 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider keyboardSpeedSlider1;
        
        #line default
        #line hidden
        
        
        #line 358 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid RoverSettings;
        
        #line default
        #line hidden
        
        
        #line 359 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Canvas ___No_Name_;
        
        #line default
        #line hidden
        
        
        #line 361 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle HomeMacroBtn;
        
        #line default
        #line hidden
        
        
        #line 362 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle SampleCollectionMacroBtn;
        
        #line default
        #line hidden
        
        
        #line 363 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle SampleDepositMacroBtn;
        
        #line default
        #line hidden
        
        
        #line 364 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle ToggleHeadlightsBtn;
        
        #line default
        #line hidden
        
        
        #line 386 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image image;
        
        #line default
        #line hidden
        
        
        #line 396 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label elapsedTime;
        
        #line default
        #line hidden
        
        
        #line 398 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button stopWatchResetBtn;
        
        #line default
        #line hidden
        
        
        #line 399 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label FrontSensor;
        
        #line default
        #line hidden
        
        
        #line 400 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label LeftSensor;
        
        #line default
        #line hidden
        
        
        #line 401 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label RightSensor;
        
        #line default
        #line hidden
        
        
        #line 407 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image wv_png;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Mars_Rover_OCU;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 7 "..\..\MainWindow.xaml"
            ((Mars_Rover_OCU.MainWindow)(target)).Closing += new System.ComponentModel.CancelEventHandler(this.Window_Closing);
            
            #line default
            #line hidden
            
            #line 7 "..\..\MainWindow.xaml"
            ((Mars_Rover_OCU.MainWindow)(target)).Loaded += new System.Windows.RoutedEventHandler(this.Window_Loaded);
            
            #line default
            #line hidden
            return;
            case 2:
            this.roverGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 3:
            this.portTxtBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 4:
            this.roverListenBtn = ((System.Windows.Controls.Button)(target));
            
            #line 69 "..\..\MainWindow.xaml"
            this.roverListenBtn.Click += new System.Windows.RoutedEventHandler(this.roverListenBtn_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.roverControlBtn = ((System.Windows.Controls.Button)(target));
            
            #line 81 "..\..\MainWindow.xaml"
            this.roverControlBtn.Click += new System.Windows.RoutedEventHandler(this.roverControlBtn_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.manualControlRB = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 7:
            this.autoControlRB = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 8:
            this.xboxControlRB = ((System.Windows.Controls.RadioButton)(target));
            
            #line 106 "..\..\MainWindow.xaml"
            this.xboxControlRB.Checked += new System.Windows.RoutedEventHandler(this.xbox_Checked);
            
            #line default
            #line hidden
            return;
            case 9:
            this.keyControlRB = ((System.Windows.Controls.RadioButton)(target));
            
            #line 107 "..\..\MainWindow.xaml"
            this.keyControlRB.Checked += new System.Windows.RoutedEventHandler(this.keyboard_Checked);
            
            #line default
            #line hidden
            return;
            case 10:
            this.logConsole = ((System.Windows.Controls.TextBox)(target));
            return;
            case 11:
            this.connectionStatusLbl = ((System.Windows.Controls.Label)(target));
            return;
            case 12:
            this.disconnectRoverBtn = ((System.Windows.Controls.Button)(target));
            
            #line 128 "..\..\MainWindow.xaml"
            this.disconnectRoverBtn.Click += new System.Windows.RoutedEventHandler(this.disconnectBtn_Click);
            
            #line default
            #line hidden
            return;
            case 13:
            this.keyboard = ((System.Windows.Controls.Grid)(target));
            return;
            case 14:
            this.forwardBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 15:
            this.reverseBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 16:
            this.leftBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 17:
            this.rightBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 18:
            this.keyboardSpeedSlider = ((System.Windows.Controls.Slider)(target));
            return;
            case 19:
            this.SpeedSensitivity = ((System.Windows.Controls.TextBox)(target));
            return;
            case 20:
            this.keyboard_Copy = ((System.Windows.Controls.Grid)(target));
            return;
            case 21:
            this.ArmUpBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 22:
            this.ArmDownBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 23:
            this.ScoopInBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 24:
            this.ScoopOutBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 25:
            this.keyboardSpeedSlider1 = ((System.Windows.Controls.Slider)(target));
            return;
            case 26:
            this.RoverSettings = ((System.Windows.Controls.Grid)(target));
            return;
            case 27:
            this.___No_Name_ = ((System.Windows.Controls.Canvas)(target));
            return;
            case 28:
            this.HomeMacroBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 29:
            this.SampleCollectionMacroBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 30:
            this.SampleDepositMacroBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 31:
            this.ToggleHeadlightsBtn = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 32:
            this.image = ((System.Windows.Controls.Image)(target));
            return;
            case 33:
            this.elapsedTime = ((System.Windows.Controls.Label)(target));
            return;
            case 34:
            this.stopWatchResetBtn = ((System.Windows.Controls.Button)(target));
            
            #line 398 "..\..\MainWindow.xaml"
            this.stopWatchResetBtn.Click += new System.Windows.RoutedEventHandler(this.stopWatchReset_Clicked);
            
            #line default
            #line hidden
            return;
            case 35:
            this.FrontSensor = ((System.Windows.Controls.Label)(target));
            return;
            case 36:
            this.LeftSensor = ((System.Windows.Controls.Label)(target));
            return;
            case 37:
            this.RightSensor = ((System.Windows.Controls.Label)(target));
            return;
            case 38:
            this.wv_png = ((System.Windows.Controls.Image)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

