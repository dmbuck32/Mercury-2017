﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Mars_Rover_OCU.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class ControllerSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static ControllerSettings defaultInstance = ((ControllerSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new ControllerSettings())));
        
        public static ControllerSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("500")]
        public int MaxSpeed {
            get {
                return ((int)(this["MaxSpeed"]));
            }
            set {
                this["MaxSpeed"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("One")]
        public global::Microsoft.Xna.Framework.PlayerIndex DrivePlayer {
            get {
                return ((global::Microsoft.Xna.Framework.PlayerIndex)(this["DrivePlayer"]));
            }
            set {
                this["DrivePlayer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Two")]
        public global::Microsoft.Xna.Framework.PlayerIndex ArmPlayer {
            get {
                return ((global::Microsoft.Xna.Framework.PlayerIndex)(this["ArmPlayer"]));
            }
            set {
                this["ArmPlayer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("500")]
        public int SteeringSensitivity {
            get {
                return ((int)(this["SteeringSensitivity"]));
            }
            set {
                this["SteeringSensitivity"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("20")]
        public int ArmSensitivity {
            get {
                return ((int)(this["ArmSensitivity"]));
            }
            set {
                this["ArmSensitivity"] = value;
            }
        }
    }
}
