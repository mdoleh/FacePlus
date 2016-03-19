using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
//using System.Reflection;
using System.IO;

namespace Mixamo {
	public static class FacePlus {
		public static volatile int FramesTracked = 0;
		public static bool IsInitStarted = false;

		private const bool shouldLoadDlls = true;
		private static volatile bool initResult = false;
		private static volatile bool trackResult = false;
		private static volatile bool trackForever = true;
		private static Thread initThread = null;
		private static Thread trackingThread = null;
		private static int deviceID;

		private static string lastError="";
		public static bool HasError = false;

		static FacePlus() {
			string faceplusFolder = @"Assets\Plugins\";
			string fallbackFolder = "";

#if UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN
			string[] dlls = new string[] {
				"OpenCL.dll", // sometimes finds the wrong OpenCL on older NVIDIA machines
				"clAmdBlas.dll",
				"opencv_core249.dll",
				"opencv_highgui249.dll", // NB: obfuscated dll has dependent dlls included, no need for above
				"faceplus.dll"
			};

			if (shouldLoadDlls) {
				foreach(var dll in dlls) {
					if (!LoadFromFolder (faceplusFolder, dll)) {
						Logger.Log ("Trying fallback folder...");
						LoadFromFolder (fallbackFolder, dll);
					}
				}
			}
#else

#endif
		}

#if UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN
		private static bool LoadFromFolder(string folder, string dll) {
			string path = Path.GetFullPath ( folder + dll );
			IntPtr ptr = LoadLibrary(path);
			Logger.Log ("DLL exists at path: " + File.Exists (path) + ". Load result: " + (ptr != IntPtr.Zero) 
			            + "\nPath:" + path 
			            + "\nCurrent directory:"+Environment.CurrentDirectory);
			return IntPtr.Zero != ptr;
		}

#endif
		public static int Login(string name, string password, string client ) {
			return faceplus_log_in (name, password, client );
		}

		public static void Logout(){
			Logger.Log ("Logging out user from faceplus");
			faceplus_log_off ();
		}

		public static string LoginString() {
			IntPtr ptr = faceplus_get_log_in_message (  );

			if(ptr!=IntPtr.Zero)
				return Marshal.PtrToStringAnsi(ptr);
			return "";
		}
		
		public static void Init(string source) {
			IsInitStarted = true;
			initThread = new Thread(() => InitSynch (source));
			initThread.Start ();
			while(!initThread.IsAlive) {} // spin while thread starts
		}

		public static bool InitBufferTracker(int width, int height, float frameRate) {
			initResult = faceplus_init_buffer_tracker(width, height, (double) frameRate, "RGB");
			
			Logger.Log ("Initialization result: " + initResult);
			return initResult;
		}

		public static bool Teardown() {
			return faceplus_teardown();
		}
		
		public static IEnumerator AfterInit(Action<bool> complete) {
			while(!IsInitComplete) yield return new WaitForSeconds(0.1f);
			//CheckForError(IsInitSuccessful);
			complete(IsInitSuccessful);
		}
		
		public static bool InitSynch(string source) {
			initResult = /*initResult ||*/ faceplus_init(source);
			CheckForError(initResult);
			return initResult;
		}
		
		public static bool IsInitComplete {
			get {
				return initThread != null 
					&& !initThread.IsAlive;
			}
		}
		
		public static bool IsInitSuccessful {
			get {
				//return IsInitComplete && initResult;
				return initResult;
			}
		}
		
		public static bool IsTracking {
			get {
				return trackResult;
			}
		}

		public static bool TrackSynchBuffer(byte[] frameBuffer,bool outputDebugImages) {
			trackResult = faceplus_synchronous_track_buffer(frameBuffer,outputDebugImages);
			if (trackResult) {
				FramesTracked++;
				UpdateCurrentVector();
			}
			CheckForError (trackResult);
			return trackResult;
		}
		
		public static bool TrackSynch() {
			trackResult = faceplus_synchronous_track();
			if (trackResult) {
				FramesTracked++;
				UpdateCurrentVector ();
			}
			CheckForError (trackResult);
			return trackResult;
		}
		
		public static IEnumerator TrackForever() {
			trackForever = true;
			while (trackForever) {
				TrackSynch ();
				yield return null;
			}
		}
		
		public static void TrackForeverThreaded() {
			trackingThread = new Thread(() => {
				trackForever = true;
				while (trackForever) TrackSynch ();
			});
			trackingThread.Start ();
			while(!trackingThread.IsAlive) {}
		}
		
		public static void StopTracking() {
			trackForever = false;
			Logger.Log ("Stopping tracking...");
			if (trackingThread != null) 
				trackingThread.Join ();

		}
		
		public static int ChannelCount {
			get {
				return faceplus_output_channels_count();
			}
		}
		
		public static string GetChannelName(int index) 
		{
			if (index < 0 || index >= ChannelCount) 
				throw new IndexOutOfRangeException();
			
			return Marshal.PtrToStringAnsi (faceplus_output_channel_name(index));
		}

		public static int DevicesCount
		{
			get
			{
				return faceplus_get_camera_devices_count();
			}
		}

		public static string GetDeviceName(int index) 
		{
			if (index < 0 || index >= DevicesCount) 
				throw new IndexOutOfRangeException();
			
			return Marshal.PtrToStringAnsi (faceplus_get_camera_device_name(index));
		}

		public static void GetCurrentVector(float[] vector) {
			faceplus_current_output_vector(vector);
		}

		private static float[] vector;
		private static object vec_lock = new object();
		public static void UpdateCurrentVector() {
			if (ChannelCount < 0)
								return;
			if (vector == null)
				vector = new float[ChannelCount];

			lock(vector) {
				faceplus_current_output_vector(vector);
			}

		}
		
		public static float[] GetCurrentVector() {
				if(ChannelCount<0) return new float[0];
			float[] copy = new float[ChannelCount];
			lock (vector) {
				vector.CopyTo (copy, 0);
			}
			return copy;
		}

		public static int Echo(int n) {
			return faceplus_echo(n);
		}

		public static void StartUp(){
			Logger.Log ("Face Plus starting up...");
		}

		public static void CheckForError(bool input){
			HasError = !input;
			IntPtr ptr = faceplus_get_error_message (  );
			lastError = (ptr != IntPtr.Zero) ? Marshal.PtrToStringAnsi (ptr) : "";
		}

		public static string GetError(){

			return lastError;
		}

		public static int GetCameraCount(){
			return faceplus_get_camera_devices_count ();		
		}

		public static string GetCameraDeviceName(int index){
			return Marshal.PtrToStringAnsi (faceplus_get_camera_device_name (index));
		}

		//wrapper for deviceID
		public static int DeviceID{
			get { return deviceID; }
			set { deviceID = value; }
		}


#if UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN
		[DllImport("kernel32.dll", CharSet=CharSet.Auto)]
		private static extern IntPtr LoadLibrary(string lpFileName);
#endif
		[DllImport("faceplus")]
		private static extern IntPtr faceplus_get_error_message();

		[DllImport("faceplus")]
		private static extern int faceplus_echo(int n);
		
		[DllImport("faceplus")]
		private static extern bool faceplus_init(string video_source);
		
		[DllImport("faceplus")]
		private static extern bool faceplus_init_buffer_tracker(int w,int h,double fps, string channels);
		
		[DllImport("faceplus")]
		private static extern bool faceplus_teardown();
		
		[DllImport("faceplus")]
		private static extern int faceplus_output_channels_count();
		
		[DllImport("faceplus")]
		private static extern bool faceplus_synchronous_track();
		
		[DllImport("faceplus")]
		private static extern bool faceplus_synchronous_track_buffer([In] byte[] buffer, bool debugImages);
		
		[DllImport("faceplus")]
		private static extern IntPtr faceplus_output_channel_name(int index);
		
		[DllImport("faceplus")]
		private static extern void faceplus_current_output_vector([Out] float[] vector);

		[DllImport("faceplus")]
		private static extern int faceplus_log_in(string user, string password, string client);

		[DllImport("faceplus")]
		private static extern void faceplus_log_off();

		[DllImport("faceplus")]
		private static extern IntPtr faceplus_get_log_in_message();

		[DllImport("faceplus")]
		private static extern int faceplus_get_camera_devices_count();

		[DllImport("faceplus")]
		private static extern IntPtr faceplus_get_camera_device_name(int i);

	}
}
