   M S B A M L   `   `   `  ���� U  QSkybound.Gecko, Version=1.9.1.0, Culture=neutral, PublicKeyToken=3209ac31600d1857G4clr-namespace:Skybound.Gecko;assembly=Skybound.GeckoSkybound.Gecko   Mars_Rover_Control#   Mars_Rover_Control.MainWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Cwf4clr-namespace:Skybound.Gecko;assembly=Skybound.Gecko     -     � Title$  Mars Rover 2014 Control��$	��768��$
��1024�����	#FF2B2E2E � WindowStartupLocation$ CenterScreen=� � 
ResizeMode$ NoResize=� � Icon$ Resources/wv.png��.���  � ColumnDefinitions �� $
��171* ��� $
��623* �.+��� $��10,10,614,10q�$��2��.@��� $��Rover��.��� -    �Name$ 	roverGrid��$
��Left=�$	��386��$��0,-10,-2,-1q�.+��� ���	#FFEEEEEE$��10q��� $��
10,10,10,0q�$	��679��$	��Top=�.+��� $��32��$	��Top=����	#FF302D27$	��366���� $	��652��$	��Top=����	#FF302D27 �� Left$	 226��
 �� Top$ 27��$	��140���� 
���   �$��1q�$��
10,10,10,0q� J� IsHitTestVisible .  $	��679��$	��Top=��� $��	20,15,0,0q�$
��Wrap=�$��ROVER��$	��Top=�$
��Left=�$��Gisha)�$��18(�
�������$��21��$��66��� $��
21,76,22,0q�$	��119��$	��Top=�	 � RowDefinitions	 �� $	��64* ��� $	��55* �.+��� $
��Left=�$��55��$	��Top=�$	��343�����	#FFFAC313$��0,54,0,0q�$��2��
 J� Effect
 3  -System.Windows.Media.Effects.DropShadowEffect     ShadowDepth$ 1Q�   
BlurRadius$ 3Q��� $
��Left=�$��47��$	��Top=�$	��343�����	#FF2A99F5
   $ 3Q�$ 1Q��� $
��Left=�$��
276,11,0,0q� �� TextWrapping$
 Wrap=�$
��1111��$��52�� �� TextAlignment$ Center=�$��11(�{�.  $��23��$	��Top=��� $��-16,-14,323,13q�.+�A� $
��Left=�$��36��
�������$	��Top=�$��36��$ 3�����	#FF2A99F5�� $��1��$
��Left=�$	��Top=�
�������$��SWItalt)�$��24(�$ 4����Z� �� $��-16,42,323,12q�$��2��.+�A� $
��Left=�$��36��
�������$	��Top=�$��36��$ 2�����	#FFFAC313�� $��2��$
��Left=�$	��Top=�
�������$��SWItalt)�$��24(�$ 2����Z� �� $��Listen��$
��Left=�$��34,9,0,0q�$��74�� � ToolTip$$ Listen for the robot over net.��$	��Top=�$��11(�$��29��$��Courier New)��� $��Port:��$
��Left=�$��
230,12,0,0q�$	��Top=�
�������$��Courier New)�$��11(��� $��Take Control��$
��Left=�$��34,2,0,0q�$	��Top=�$	��105��$��Courier New)�$��29��$��11(�{�.  $��1��	� $��Manual��$
��Left=�$��	237,2,0,0q�$	��Top=� d� 	IsChecked$
 TrueY�$��Courier New)�
�������$��11(�{�.  $��1��	� $
��Auto��$
��Left=�$��
238,21,0,0q�$	��Top=� J� RenderTransformOrigin$ 0.494,1.111&�$ FalseY�$��Courier New)�
�������$��11(�{�.  $��1���� $��Not Connected��$��264,14,30,0q�
���  ��$��11(�$��51��$	��Top=���Z� �� $�� ��$
��Left=�$��
209,49,0,0q�$	��Top=�$��20��$��20������  �� ImageSource$ Resources/Gear.png���� $��Settings��$
��Left=�$��
146,49,0,0q�$	��Top=�$��Courier New)�$��11(�$��22��$��63���� $��
Disconnect��$
��Left=�$��
269,39,0,0q�$��76��{�.  $��26��$	��Top=��� $��Base Station��.��� ���	#FFE5E5E5�� $��1��$
��Left=�$	��720��$��
190,10,0,0q�$	��Top=�$	��599��.@��� $
��Live��{�.  .��� ���	#FFE5E5E5�� $��Mission Planner�� �� 
IsSelected . .��� ���	#FFE5E5E5$	��688��$	��Top=�.+�� $
��Left=�$	��688��$	��Top=�$	��589��.+��� $
��Left=�$��23��$��365,529,0,0q�$
 Wrap=�$	��Top=�$��68���� $
��Left=�$��23��$��511,529,0,0q�$
 Wrap=�$	��Top=�$��68���� $��	Latitude:��$
��Left=�$��302,526,0,0q�$	��Top=�$��58���� $��
Longitude:��$
��Left=�$��438,526,0,0q�$	��Top=�$��68���� -   $
��Left=�$��151,527,0,0q�$	��Top=�$	��120�� �� SelectedIndex$ 0��.@��� $��JSC - Houston���� $��WVU - Engineering���� $��Jason's House���� $��Predefined Locations:��$
��Left=�$��
20,524,0,0q�$	��Top=�$	��126��7  1System.Windows.Forms.Integration.WindowsFormsHost  -   $��	wfhSample��$��Right=�$	��589��$	��519��$	��Top=�   Child $   Skybound.Gecko.GeckoWebBrowser  -   plannerBrowser��&   System.Windows.Controls.DataGrid  $
��Left=�$��-158,226,0,0q�$	��Top=�