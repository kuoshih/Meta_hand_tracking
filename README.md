![alt text](https://github.com/kuoshih/Meta_hand_tracking/blob/main/hand_tracking_system.jpg)   

## Abstract
This code is to track hands via Meta2 HMD.
It includes:
1. tracking hands via Meta 2 SDK
2. display blue balls on hands
3. save gead 3D info, two hands 3D info, meta camrea images, AR images

## About us

Developer:   
* Kuo-Shih Tseng   
Contact: kuoshih@math.ncu.edu.tw   
Date:  2026/04/01  
License: Apache 2.0  

## Run the code   
Press "Play" button in Unity HMI.

If you want to record the data, press "Start" button in Game HMI and press "Stop" unitil finishing recording. <br>
Press "A" to enable/disable arrow displayer. <br><br><br>

## Data Folder format  
The data format is as follows:  
Assets/  
&nbsp; HandPoseLogs/  
&nbsp; &nbsp; session_20260331_xxxxx/  
&nbsp; &nbsp; &nbsp;        images/  
&nbsp; &nbsp; &nbsp;&nbsp;        frame_000000.jpg  
&nbsp; &nbsp; &nbsp;       ARimages/  
&nbsp; &nbsp; &nbsp;&nbsp;         ARframe_000000.jpg  
&nbsp; &nbsp; &nbsp;       logs/  
&nbsp; &nbsp; &nbsp;&nbsp;         handpose_log.csv  

## handpose_log data format  
**Time data**: record_frame	unity_time	unix_time	system_time_local	system_time_utc	raw_image_filename	ar_image_filename <br>     	
**Head data**: head_x	head_y	head_z	head_roll	head_pitch	head_yaw  <br>   	
**Left hand data**: left_palm_x	left_palm_y	left_palm_z	left_top_x	left_top_y	left_top_z	left_dir_x	left_dir_y	left_dir_z    <br>  	
**Right hand data**: right_palm_x	right_palm_y	right_palm_z	right_top_x	right_top_y	right_top_z	right_dir_x	right_dir_y	right_dir_z  <br>    

 
