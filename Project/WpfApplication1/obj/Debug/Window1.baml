   M S B A M L   `   `   `  ����  WpfApplication1    WpfApplication1.Window1   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  
Отчет��5   	   $	��300��6   $	��490��6$   .��5      � .+�5   
   � .+�5      &   System.Windows.Controls.DataGrid  -   5        $��
gridResult��5         SelectionMode$ Extended=�6)      AutoGenerateColumns .  $��10,10,10,35q�6_      Columns 5      0  *System.Windows.Controls.DataGridTextColumn  5	      ,  &System.Windows.Controls.DataGridColumn   Header$ Алгоритм��6)   1  +System.Windows.Controls.DataGridBoundColumn   Binding 6;   ��  �� Path$ name_algoritm�   ElementStyle 5
      �� 5       �� 
TargetType" ��6$   .�5   "   �� 	 �� Property	 � ��
 �� Value
 6?   �� $ colour�6_   5      5        5      $ Время работы��6)    6?   �� $ 	time_work� 5      �� 5      " ��6$   .�5   "   �� 	 � ��
 6?   �� $ colour�6_   5      5        5      $ Расстояние, м��6)    6@   �� $ length� 5      �� 5      " ��6$   .�5   "   �� 	 � ��
 6?   �� $ colour�6_   5      5        5      $! Временной срез��6)    6A   �� $ t� 5      �� 5       " ��6$   .�5!   "   �� 	 � ��
 6?   �� $ colour�6_   5"      5$        5%      $# Расчетное время��6)    6B   �� $ time_motion� 5&      �� 5'      " ��6$   .�5(   "   �� 	 � ��
 6?   �� $ colour�6_   5)      5+      52      �� 53      -   5        $��btn_view_way��53      $��Отобразить��6)   $
��Left=�6O   $��
10,230,0,0q�6j   $��Center=�6~   $��75��6�   55      56      57      